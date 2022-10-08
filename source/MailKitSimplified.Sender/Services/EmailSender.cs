using System;
using System.IO;
using System.Net;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Collections.Generic;
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
using MailKitSimplified.Sender.Abstractions;

namespace MailKitSimplified.Sender.Services
{
    public class EmailSender : IMimeEmailSender
    {
        private readonly EmailSenderOptions _senderOptions;
        private readonly IProtocolLogger _smtpLogger;
        private readonly SmtpClient _smtpClient;
        private readonly ILogger _logger;
        private readonly IMimeAttachmentHandler _attachmentHandler;

        public EmailSender(IOptions<EmailSenderOptions> senderOptions, ILoggerFactory loggerFactory = null, IMimeAttachmentHandler mimeAttachmentHandler = null)
        {
            _senderOptions = senderOptions?.Value ?? throw new ArgumentException(nameof(senderOptions));
            if (string.IsNullOrWhiteSpace(_senderOptions.SmtpHost))
                throw new NullReferenceException(nameof(_senderOptions.SmtpHost));
            if (!string.IsNullOrEmpty(_senderOptions.ProtocolLog))
                Directory.CreateDirectory(Path.GetDirectoryName(_senderOptions.ProtocolLog));
            _smtpLogger = _senderOptions.ProtocolLog == null ? null :
                string.IsNullOrEmpty(_senderOptions.ProtocolLog) ?
                    new ProtocolLogger(Console.OpenStandardError()) :
                        new ProtocolLogger(_senderOptions.ProtocolLog);
            _smtpClient = _smtpLogger != null ? new SmtpClient(_smtpLogger) : new SmtpClient();
            _logger = loggerFactory?.CreateLogger<EmailSender>() ?? NullLogger<EmailSender>.Instance;
            _attachmentHandler = mimeAttachmentHandler ?? new MimeAttachmentHandler(loggerFactory?.CreateLogger<MimeAttachmentHandler>(), new FileHandler(loggerFactory?.CreateLogger<FileHandler>()));
        }

        public static EmailSender Create(string smtpHost, int smtpPort = 0, NetworkCredential smtpCredential = null, string protocolLog = null)
        {
            var senderOptions = new EmailSenderOptions(smtpHost, smtpPort, smtpCredential, protocolLog);
            var options = Options.Create(senderOptions);
            var sender = new EmailSender(options);
            return sender;
        }

        private Email CreateEmail => Services.Email.CreateFrom(this);

        public IEmailWriter WriteEmail => CreateEmail.Write;

        public MimeEmailWriter MimeEmail => MimeEmailWriter.CreateFrom(this);


        [Obsolete("This method will be removed in a future version, use IEmailWriter WriteEmail instead.")]
        public IEmail Email(string fromAddress, string toAddress, string subject = "", string body = "", bool isHtml = true, params string[] attachmentFilePaths)
        {
            return CreateEmail.HandWrite(fromAddress, toAddress, subject, body, isHtml, attachmentFilePaths);
        }

        public static async Task<MimeMessage> ConvertToMimeMessage(IEmail email, IMimeAttachmentHandler attachmentHandler, CancellationToken cancellationToken = default)
        {
            var mimeMessage = new MimeMessage();

            var from = new MailboxAddress(email.From.Name, email.From.Address);
            mimeMessage.From.Add(from);
            mimeMessage.ReplyTo.Add(from);

            var to = email.To.Select(t => new MailboxAddress(t.Name, t.Address));
            mimeMessage.To.AddRange(to);

            mimeMessage.Subject = email.Subject ?? string.Empty;

            mimeMessage.Body = new TextPart(TextFormat.Html) { Text = email.Body ?? string.Empty };

            await AddAttachments(mimeMessage, attachmentHandler, email.AttachmentFilePaths, cancellationToken).ConfigureAwait(false);

            return mimeMessage;
        }

        public static async Task<MimeMessage> AddAttachments(MimeMessage mimeMessage, IMimeAttachmentHandler attachmentHandler, IEnumerable<string> filePaths, CancellationToken cancellationToken = default)
        {
            var mimeParts = await attachmentHandler.LoadFilePathsAsync(filePaths, cancellationToken).ConfigureAwait(false);
            if (mimeMessage != null && mimeParts.Any())
            {
                var multipart = new Multipart();
                if (mimeMessage.Body != null)
                    multipart.Add(mimeMessage.Body);
                foreach (var mimePart in mimeParts)
                    multipart.Add(mimePart);
                mimeMessage.Body = multipart;
            }
            return mimeMessage;
        }

        private static bool HasCircularReference(MimeMessage mimeMessage)
        {
            bool isCircular = false;
            if (mimeMessage != null && mimeMessage.From != null && mimeMessage.To != null)
            {
                var to = mimeMessage.To.Mailboxes.Select(a => a.Address.ToLower());
                var from = mimeMessage.From.Mailboxes.Select(a => a.Address.ToLower());
                isCircular = to.Intersect(from).Any();
            }
            return isCircular;
        }

        private static bool HasCircularReference(IEmail email)
        {
            bool isCircular = false;
            if (email != null && email.From != null && email.To != null)
                isCircular = email.To.Any(t => t.Address.Equals(email.From.Address, StringComparison.OrdinalIgnoreCase));
            return isCircular;
        }

        private void ValidateEmail(IEmail email)
        {
            if (email is null)
                throw new ArgumentNullException(nameof(email));
            if (email.To.Count == 0)
                throw new MissingMemberException(nameof(IEmail), nameof(IEmail.To));
            if (HasCircularReference(email))
                _logger.LogWarning("Circular reference, ToEmailAddress == FromEmailAddress");
            if (!email.From.Address.Contains("@"))
                _logger.LogWarning($"From address is invalid ({email.From})");
            foreach (var to in email.To)
                if (!to.Address.Contains("@"))
                    _logger.LogWarning($"To address is invalid ({to})");
        }

        public async Task ConnectSmtpClient(CancellationToken cancellationToken = default)
        {
            if (!_smtpClient.IsConnected && !string.IsNullOrEmpty(_senderOptions.SmtpHost))
                await _smtpClient.ConnectAsync(_senderOptions.SmtpHost, _senderOptions.SmtpPort, cancellationToken: cancellationToken).ConfigureAwait(false);
            if (_senderOptions.SmtpCredential != null && _senderOptions.SmtpCredential != default && !_smtpClient.IsAuthenticated)
                await _smtpClient.AuthenticateAsync(_senderOptions.SmtpCredential, cancellationToken).ConfigureAwait(false);
        }

        public async Task SendAsync(MimeMessage mimeMessage, IEnumerable<string> attachmentFilePaths, CancellationToken cancellationToken = default)
        {
            await AddAttachments(mimeMessage, _attachmentHandler, attachmentFilePaths, cancellationToken).ConfigureAwait(false);
            await SendAsync(mimeMessage, cancellationToken).ConfigureAwait(false);
        }

        public async Task SendAsync(MimeMessage mimeMessage, CancellationToken cancellationToken = default)
        {
            if (HasCircularReference(mimeMessage))
                _logger.LogWarning("Circular reference, ToEmailAddress == FromEmailAddress");
            await ConnectSmtpClient(cancellationToken).ConfigureAwait(false);
            Debug.WriteLine("Sending to: {0}, subject: '{1}'", mimeMessage.To, mimeMessage.Subject);
            string serverResponse = await _smtpClient.SendAsync(mimeMessage, cancellationToken).ConfigureAwait(false);
            Debug.WriteLine(serverResponse);
        }

        public async Task SendAsync(IEmail email, CancellationToken cancellationToken = default)
        {
            ValidateEmail(email);
            var mimeMessage = await ConvertToMimeMessage(email, _attachmentHandler, cancellationToken).ConfigureAwait(false);
            await ConnectSmtpClient(cancellationToken).ConfigureAwait(false);
            Debug.WriteLine("Sending email {0}", email);
            string serverResponse = await _smtpClient.SendAsync(mimeMessage, cancellationToken).ConfigureAwait(false);
            Debug.WriteLine(serverResponse);
        }

        public async Task<bool> TrySendAsync(IEmail email, CancellationToken cancellationToken = default)
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

        public void DisconnectSmtpClient()
        {
            if (_smtpClient?.IsConnected ?? false)
                _smtpClient?.Disconnect(true);
        }

        public void Dispose()
        {
            DisconnectSmtpClient();
            _smtpClient?.Dispose();
            _smtpLogger?.Dispose();
        }
    }
}