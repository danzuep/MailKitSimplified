using MimeKit;
using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using System;
using System.Linq;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MailKitSimplified.Sender.Abstractions;
using MailKitSimplified.Sender.Extensions;
using MailKitSimplified.Email.Models;
using MailKitSimplified.Email.Extensions;

namespace MailKitSimplified.Email.Services
{
    /// <inheritdoc cref="ISmtpSender" />
    public sealed class SmtpSender
    {
        private readonly EmailOptions _senderOptions;
        private readonly ISmtpClient _smtpClient;
        private readonly ILogger<SmtpSender> _logger;

        /// <inheritdoc cref="ISmtpSender" />
        public SmtpSender(IOptions<EmailOptions> senderOptions, ISmtpClient smtpClient = null, ILogger<SmtpSender> logger = null)
        {
            _senderOptions = senderOptions.Value;
            if (string.IsNullOrWhiteSpace(_senderOptions.Host))
                throw new ArgumentException($"{nameof(EmailOptions.Host)} is not set.");
            _smtpClient = smtpClient ?? _senderOptions.SmtpClient.Value;
            _logger = logger ?? NullLogger<SmtpSender>.Instance;
        }

        public ISmtpClient SmtpClient => _smtpClient;

        public static bool ValidateEmailAddresses(IEnumerable<string> sourceEmailAddresses, IEnumerable<string> destinationEmailAddresses, ILogger logger)
        {
            if (sourceEmailAddresses is null)
                throw new ArgumentNullException(nameof(sourceEmailAddresses));
            if (destinationEmailAddresses is null)
                throw new ArgumentNullException(nameof(destinationEmailAddresses));
            if (logger is null)
                logger = NullLogger.Instance;
            bool isValid = true;
            int sourceEmailAddressCount = 0, destinationEmailAddressCount = 0;
            foreach (var from in sourceEmailAddresses)
            {
                if (!from.Contains('@'))
                {
                    logger.LogWarning($"From address is invalid ({from})");
                    isValid = false;
                }
                foreach (var to in destinationEmailAddresses)
                {
                    if (!to.Contains('@'))
                    {
                        logger.LogWarning($"To address is invalid ({to})");
                        isValid = false;
                    }
                    if (to.Equals(from, StringComparison.OrdinalIgnoreCase))
                    {
                        logger.LogWarning($"Circular reference, To ({to}) == From ({from})");
                        isValid = false;
                    }
                    destinationEmailAddressCount++;
                }
                sourceEmailAddressCount++;
            }
            if (sourceEmailAddressCount == 0)
                logger.LogWarning("Source email address not specified");
            else if (destinationEmailAddressCount == 0)
                logger.LogWarning("Destination email address not specified");
            isValid &= sourceEmailAddressCount > 0 && destinationEmailAddressCount > 0;
            return isValid;
        }

        public static bool ValidateMimeMessage(MimeMessage mimeMessage, ILogger logger = null)
        {
            bool isValid = false;
            if (mimeMessage != null)
            {
                if (!mimeMessage.BodyParts.Any())
                    mimeMessage.Body = new TextPart { Text = string.Empty };
                var from = mimeMessage.From.Mailboxes.Select(m => m.Address);
                var toCcBcc = mimeMessage.To.Mailboxes.Select(m => m.Address)
                    .Concat(mimeMessage.Cc.Mailboxes.Select(m => m.Address))
                    .Concat(mimeMessage.Bcc.Mailboxes.Select(m => m.Address));
                isValid = ValidateEmailAddresses(from, toCcBcc, logger);
                if (mimeMessage.ReplyTo.Count == 0 && mimeMessage.From.Count == 0)
                    mimeMessage.ReplyTo.Add(new MailboxAddress("Unmonitored", $"noreply@localhost"));
                if (mimeMessage.From.Count == 0)
                    mimeMessage.From.Add(new MailboxAddress("LocalHost", $"{Guid.NewGuid():N}@localhost"));
            }
            return isValid;
        }

        public static string GetEnvelope(MimeMessage mimeMessage, bool includeTextBody = false)
        {
            string envelope = string.Empty;
            using (var text = new StringWriter())
            {
                text.Write("Message-Id: {0}. ", mimeMessage.MessageId);
                text.Write("Date: {0}. ", mimeMessage.Date);
                if (mimeMessage.From.Count > 0)
                    text.Write("From: {0}. ", string.Join("; ", mimeMessage.From));
                if (mimeMessage.To.Count > 0)
                    text.Write("To: {0}. ", string.Join("; ", mimeMessage.To));
                if (mimeMessage.Cc.Count > 0)
                    text.Write("Cc: {0}. ", string.Join("; ", mimeMessage.Cc));
                if (mimeMessage.Bcc.Count > 0)
                    text.Write("Bcc: {0}. ", string.Join("; ", mimeMessage.Bcc));
                text.Write("Subject: \"{0}\". ", mimeMessage.Subject);
                var attachmentCount = mimeMessage.Attachments.Count();
                if (attachmentCount > 0)
                    text.Write("{0} Attachment{1}: '{2}'. ",
                        attachmentCount, attachmentCount == 1 ? "" : "s",
                        string.Join("', '", mimeMessage.Attachments.GetAttachmentNames()));
                if (includeTextBody && mimeMessage.TextBody?.Length > 0)
                    text.Write($"TextBody: \"{mimeMessage.TextBody}\". ");
                envelope = text.ToString();
            }
            return envelope;
        }

        public async Task SendAsync(MimeMessage mimeMessage, CancellationToken cancellationToken = default, ITransferProgress transferProgress = null)
        {
            _ = ValidateMimeMessage(mimeMessage, _logger);
            await _smtpClient.CheckAsync(_senderOptions, cancellationToken);
            _logger.LogTrace($"Sending {GetEnvelope(mimeMessage, includeTextBody: true)}");
            string serverResponse = await _smtpClient.SendAsync(mimeMessage, cancellationToken, transferProgress).ConfigureAwait(false);
            _logger.LogTrace($"{_senderOptions} server response: \"{serverResponse}\".");
            _logger.LogDebug($"Sent Message-ID {mimeMessage.MessageId}.");
        }

        public async Task<bool> TrySendAsync(MimeMessage mimeMessage, CancellationToken cancellationToken = default, ITransferProgress transferProgress = null)
        {
            bool isSent = false;
            try
            {
                await SendAsync(mimeMessage, cancellationToken, transferProgress).ConfigureAwait(false);
                isSent = true;
            }
            catch (AuthenticationException ex)
            {
                _logger.LogError(ex, $"Failed to authenticate with mail server. {_senderOptions}");
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, $"Failed to connect to mail server. {_senderOptions}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send email. {mimeMessage}");
            }
            return isSent;
        }

        public ISmtpSender Copy() => MemberwiseClone() as ISmtpSender;

        public override string ToString() => _senderOptions.ToString();

        public async Task DisconnectAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogTrace("Disconnecting SMTP email client...");
            if (_smtpClient.IsConnected)
                await _smtpClient.DisconnectAsync(true, cancellationToken).ConfigureAwait(false);
        }

        public async ValueTask DisposeAsync()
        {
            await DisconnectAsync().ConfigureAwait(false);
            _smtpClient.Dispose();
        }

        public void Dispose()
        {
            DisconnectAsync().GetAwaiter().GetResult();
            _smtpClient.Dispose();
        }
    }
}