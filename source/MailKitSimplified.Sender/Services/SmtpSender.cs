using MimeKit;
using MailKit;
using MailKit.Net.Smtp;
using System;
using System.IO;
using System.Net;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.IO.Abstractions;
using System.Collections.Generic;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MailKitSimplified.Sender.Abstractions;
using MailKitSimplified.Sender.Extensions;
using MailKitSimplified.Sender.Models;

namespace MailKitSimplified.Sender.Services
{
    public sealed class SmtpSender : ISmtpSender
    {
        private readonly ILogger<SmtpSender> _logger;
        private readonly ISmtpClient _smtpClient;
        private readonly EmailSenderOptions _senderOptions;

        public SmtpSender(IOptions<EmailSenderOptions> senderOptions, ILogger<SmtpSender> logger = null, IProtocolLogger protocolLogger = null, ISmtpClient smtpClient = null)
        {
            _logger = logger ?? NullLogger<SmtpSender>.Instance;
            _senderOptions = senderOptions.Value;
            if (string.IsNullOrWhiteSpace(_senderOptions.SmtpHost))
                throw new NullReferenceException(nameof(EmailSenderOptions.SmtpHost));
            var smtpLogger = protocolLogger ?? GetProtocolLogger(smtpClient == null ? _senderOptions.ProtocolLog : null);
            _smtpClient = smtpClient ?? (smtpLogger !=null ? new SmtpClient(smtpLogger) : new SmtpClient());
        }

        public static SmtpSender Create(string smtpHost, ushort smtpPort = 0, string username = null, string password = null, string protocolLog = null)
        {
            var smtpCredential = new NetworkCredential(username, password);
            var sender = Create(smtpHost, smtpCredential, smtpPort, protocolLog);
            return sender;
        }

        public static SmtpSender Create(string smtpHost, NetworkCredential smtpCredential, ushort smtpPort = 0, string protocolLog = null)
        {
            var senderOptions = new EmailSenderOptions(smtpHost, smtpCredential, smtpPort, protocolLog);
            var sender = Create(senderOptions);
            return sender;
        }

        public static SmtpSender Create(EmailSenderOptions emailSenderOptions, ILogger<SmtpSender> logger = null)
        {
            var senderOptions = Options.Create(emailSenderOptions);
            var sender = new SmtpSender(senderOptions, logger);
            return sender;
        }

        public SmtpSender SetProtocolLog(string logFilePath)
        {
            _senderOptions.ProtocolLog = logFilePath;
            var sender = Create(_senderOptions, _logger);
            return sender;
        }

        public SmtpSender SetPort(ushort smtpPort)
        {
            _senderOptions.SmtpPort = smtpPort;
            return this;
        }

        public SmtpSender SetCredential(string username, string password)
        {
            _senderOptions.SmtpCredential = new NetworkCredential(username, password);
            return this;
        }

        public IEmailWriter WriteEmail => new EmailWriter(this);

        public static IProtocolLogger GetProtocolLogger(string logFilePath = null, IFileSystem fileSystem = null)
        {
            IProtocolLogger protocolLogger = null;
            if (logFilePath?.Equals("console", StringComparison.OrdinalIgnoreCase) ?? false)
            {
                protocolLogger = new ProtocolLogger(Console.OpenStandardError());
            }
            else if (!string.IsNullOrWhiteSpace(logFilePath))
            {
                var _fileSystem = fileSystem ?? new FileSystem();
                var directoryName = _fileSystem.Path.GetDirectoryName(logFilePath);
                if (!string.IsNullOrWhiteSpace(directoryName))
                    _fileSystem.Directory.CreateDirectory(directoryName);
                protocolLogger = new ProtocolLogger(logFilePath);
            }
            return protocolLogger;
        }

        public async ValueTask<ISmtpClient> ConnectSmtpClientAsync(CancellationToken cancellationToken = default)
        {
            if (!_smtpClient.IsConnected && !string.IsNullOrEmpty(_senderOptions.SmtpHost))
            {
                await _smtpClient.ConnectAsync(_senderOptions.SmtpHost, _senderOptions.SmtpPort, cancellationToken: cancellationToken).ConfigureAwait(false);
                _logger.LogTrace($"SMTP client connected to {_senderOptions.SmtpHost}.");
            }
            if (_senderOptions.SmtpCredential != null && !_smtpClient.IsAuthenticated)
            {
                await _smtpClient.AuthenticateAsync(_senderOptions.SmtpCredential, cancellationToken).ConfigureAwait(false);
                _logger.LogTrace($"SMTP client authenticated with {_senderOptions.SmtpHost}.");
            }
            return _smtpClient;
        }

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
                if (!from.Contains("@"))
                {
                    logger.LogWarning($"From address is invalid ({from})");
                    isValid = false;
                }
                foreach (var to in destinationEmailAddresses)
                {
                    if (!to.Contains("@"))
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
            _ = await ConnectSmtpClientAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogTrace($"Sending {GetEnvelope(mimeMessage, includeTextBody: true)}");
            string serverResponse = await _smtpClient.SendAsync(mimeMessage, cancellationToken, transferProgress).ConfigureAwait(false);
            _logger.LogTrace($"Server response: \"{serverResponse}\".");
        }

        public async Task<bool> TrySendAsync(MimeMessage mimeMessage, CancellationToken cancellationToken = default, ITransferProgress transferProgress = null)
        {
            bool isSent = false;
            try
            {
                await SendAsync(mimeMessage, cancellationToken, transferProgress).ConfigureAwait(false);
                isSent = true;
            }
            catch (MailKit.Security.AuthenticationException ex)
            {
                _logger.LogError(ex, "Failed to authenticate with mail server.");
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Failed to connect to mail server.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email.");
            }
            return isSent;
        }

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