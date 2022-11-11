using System;
using System.IO;
using System.Net;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MimeKit;
using MimeKit.Text;
using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using MailKitSimplified.Core.Abstractions;
using MailKitSimplified.Core.Models;
using MailKitSimplified.Core.Services;
using MailKitSimplified.Sender.Abstractions;

namespace MailKitSimplified.Sender.Services
{
    public class MimeMessageSender : IMimeMessageSender
    {
        private readonly ILogger _logger;
        private readonly ISmtpClient _smtpClient;
        private readonly EmailSenderOptions _senderOptions;
        private readonly IMimeAttachmentHandler _attachmentHandler;

        public MimeMessageSender(IOptions<EmailSenderOptions> senderOptions, IMimeAttachmentHandler mimeAttachmentHandler = null, ILogger<MimeMessageSender> logger = null)
        {
            _logger = logger ?? NullLogger<MimeMessageSender>.Instance;
            _senderOptions = senderOptions.Value;
            if (string.IsNullOrWhiteSpace(_senderOptions.SmtpHost))
                throw new NullReferenceException(nameof(EmailSenderOptions.SmtpHost));
            _attachmentHandler = mimeAttachmentHandler ?? new MimeAttachmentHandler();
            var smtpLogger = GetProtocolLogger(_senderOptions.ProtocolLog);
            _smtpClient = smtpLogger != null ? new SmtpClient(smtpLogger) : new SmtpClient();
        }

        public static MimeMessageSender Create(string smtpHost, ushort smtpPort = 0, string username = null, string password = null, string protocolLog = null)
        {
            var smtpCredential = username == null && password == null ? null : new NetworkCredential(username ?? "", password ?? "");
            var sender = Create(smtpHost, smtpCredential, smtpPort, protocolLog);
            return sender;
        }

        public static MimeMessageSender Create(string smtpHost, NetworkCredential smtpCredential, ushort smtpPort = 0, string protocolLog = null)
        {
            var senderOptions = new EmailSenderOptions(smtpHost, smtpCredential, smtpPort, protocolLog);
            var options = Options.Create(senderOptions);
            var sender = new MimeMessageSender(options);
            return sender;
        }

        public IEmailWriter WriteEmail => Email.Create(this);

        public static IProtocolLogger GetProtocolLogger(string logFilePath = null, IFileSystem fileSystem = null)
        {
            if (!string.IsNullOrEmpty(logFilePath))
            {
                if (fileSystem == null)
                    fileSystem = new FileSystem();
                var directoryName = fileSystem.Path.GetDirectoryName(logFilePath);
                fileSystem.Directory.CreateDirectory(directoryName);
            }
            var protocolLogger = logFilePath == null ? null :
                string.IsNullOrWhiteSpace(logFilePath) ?
                    new ProtocolLogger(Console.OpenStandardError()) :
                        new ProtocolLogger(logFilePath);
            return protocolLogger;
        }

        public static MimeMessage ConvertToMimeMessage(ISendableEmail email)
        {
            var mimeMessage = new MimeMessage();

            var from = email.From.Select(m => new MailboxAddress(m.Name, m.Email));
            mimeMessage.From.AddRange(from);
            mimeMessage.ReplyTo.AddRange(from);

            var to = email.To.Select(m => new MailboxAddress(m.Name, m.Email));
            mimeMessage.To.AddRange(to);

            var cc = email.Cc.Select(m => new MailboxAddress(m.Name, m.Email));
            mimeMessage.Cc.AddRange(cc);

            var bcc = email.Bcc.Select(m => new MailboxAddress(m.Name, m.Email));
            mimeMessage.Bcc.AddRange(bcc);

            mimeMessage.Subject = email.Subject ?? string.Empty;

            mimeMessage.Body = new TextPart(TextFormat.Html) { Text = email.Body ?? string.Empty };

            return mimeMessage;
        }

        public async Task<MimeMessage> ConvertToMimeMessageAsync(ISendableEmail email, CancellationToken cancellationToken = default)
        {
            var mimeMessage = ConvertToMimeMessage(email);
            mimeMessage = await _attachmentHandler.AddAttachmentsAsync(mimeMessage, email.AttachmentFilePaths, cancellationToken).ConfigureAwait(false);
            return mimeMessage;
        }

        private bool ValidateMimeMessage(MimeMessage mimeMessage)
        {
            bool isValid = false;
            if (mimeMessage != null)
            {
                if (mimeMessage.From.Count == 0)
                    mimeMessage.From.Add(new MailboxAddress("localhost", $"{Guid.NewGuid():N}@localhost"));
                if (!mimeMessage.BodyParts.Any())
                    mimeMessage.Body = new TextPart { Text = string.Empty };
                var from = mimeMessage.From.Mailboxes.Select(m => m.Address);
                var toCcBcc = mimeMessage.To.Mailboxes.Select(m => m.Address)
                    .Concat(mimeMessage.Cc.Mailboxes.Select(m => m.Address))
                    .Concat(mimeMessage.Bcc.Mailboxes.Select(m => m.Address));
                isValid = EmailContact.ValidateEmailAddresses(from, toCcBcc, _logger);
            }
            return isValid;
        }

        public async Task ConnectSmtpClientAsync(CancellationToken cancellationToken = default)
        {
            if (!_smtpClient.IsConnected && !string.IsNullOrEmpty(_senderOptions.SmtpHost))
                await _smtpClient.ConnectAsync(_senderOptions.SmtpHost, _senderOptions.SmtpPort, cancellationToken: cancellationToken).ConfigureAwait(false);
            if (_senderOptions.SmtpCredential != null && _senderOptions.SmtpCredential != default && !_smtpClient.IsAuthenticated)
                await _smtpClient.AuthenticateAsync(_senderOptions.SmtpCredential, cancellationToken).ConfigureAwait(false);
        }

        public async Task SendAsync(MimeMessage mimeMessage, IEnumerable<string> attachmentFilePaths, CancellationToken cancellationToken = default)
        {
            await _attachmentHandler.AddAttachmentsAsync(mimeMessage, attachmentFilePaths, cancellationToken).ConfigureAwait(false);
            await SendAsync(mimeMessage, cancellationToken).ConfigureAwait(false);
        }

        public async Task SendAsync(MimeMessage mimeMessage, CancellationToken cancellationToken = default)
        {
            _ = ValidateMimeMessage(mimeMessage);
            await ConnectSmtpClientAsync(cancellationToken).ConfigureAwait(false);
            Debug.WriteLine("Sending to: {0}, subject: '{1}'", mimeMessage.To, mimeMessage.Subject);
            string serverResponse = await _smtpClient.SendAsync(mimeMessage, cancellationToken).ConfigureAwait(false);
            Debug.WriteLine(serverResponse);
        }

        public async Task SendAsync(ISendableEmail email, CancellationToken cancellationToken = default)
        {
            var mimeMessage = await ConvertToMimeMessageAsync(email, cancellationToken).ConfigureAwait(false);
            _ = ValidateMimeMessage(mimeMessage);
            await ConnectSmtpClientAsync(cancellationToken).ConfigureAwait(false);
            Debug.WriteLine("Sending email {0}", email.ToString());
            string serverResponse = await _smtpClient.SendAsync(mimeMessage, cancellationToken).ConfigureAwait(false);
            Debug.WriteLine(serverResponse);
        }

        public async Task<bool> TrySendAsync(ISendableEmail email, CancellationToken cancellationToken = default)
        {
            bool isSent = false;
            try
            {
                await SendAsync(email, cancellationToken).ConfigureAwait(false);
                isSent = true;
            }
            catch (AuthenticationException ex)
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

        public async Task<bool> TrySendAsync(MimeMessage mimeMessage, CancellationToken cancellationToken = default) =>
            await TrySendAsync(mimeMessage, Array.Empty<string>(), cancellationToken).ConfigureAwait(false);


        public async Task<bool> TrySendAsync(MimeMessage mimeMessage, IList<string> attachmentFilePaths, CancellationToken cancellationToken)
        {
            bool isSent = false;
            try
            {
                await SendAsync(mimeMessage, attachmentFilePaths, cancellationToken).ConfigureAwait(false);
                isSent = true;
            }
            catch (AuthenticationException ex)
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

        public void DisconnectSmtpClient()
        {
            if (_smtpClient?.IsConnected ?? false)
                _smtpClient?.Disconnect(true);
        }

        public void Dispose()
        {
            DisconnectSmtpClient();
            _smtpClient?.Dispose();
        }
    }
}