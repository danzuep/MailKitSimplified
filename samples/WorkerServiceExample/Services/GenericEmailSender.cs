using MimeKit;
using MailKit;
using MailKit.Net.Smtp;
using System.Net;
using System.IO.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MailKitSimplified.Core.Abstractions;
using MailKitSimplified.Core.Services;
using MailKitSimplified.Sender.Models;

namespace WorkerServiceExample.Services
{
    internal class GenericEmailSender : IGenericEmailSender
    {
        private readonly ILogger<GenericEmailSender> _logger;
        private readonly ISmtpClient _smtpClient;
        private readonly EmailSenderOptions _senderOptions;

        public GenericEmailSender(IOptions<EmailSenderOptions> senderOptions, ILogger<GenericEmailSender>? logger = null, IProtocolLogger? protocolLogger = null, ISmtpClient? smtpClient = null)
        {
            _logger = logger ?? NullLogger<GenericEmailSender>.Instance;
            _senderOptions = senderOptions.Value;
            if (string.IsNullOrWhiteSpace(_senderOptions.SmtpHost))
                throw new NullReferenceException(nameof(EmailSenderOptions.SmtpHost));
            var smtpLogger = protocolLogger ?? GetProtocolLogger(smtpClient == null ? _senderOptions.ProtocolLog : null);
            _smtpClient = smtpClient ?? (smtpLogger != null ? new SmtpClient(smtpLogger) : new SmtpClient());
        }

        public static GenericEmailSender Create(string smtpHost, ILogger<GenericEmailSender>? logger = null)
        {
            var emailSenderOptions = new EmailSenderOptions(smtpHost);
            var sender = Create(emailSenderOptions, logger);
            return sender;
        }

        public static GenericEmailSender Create(EmailSenderOptions emailSenderOptions, ILogger<GenericEmailSender>? logger = null)
        {
            var senderOptions = Options.Create(emailSenderOptions);
            var sender = new GenericEmailSender(senderOptions, logger);
            return sender;
        }

        public GenericEmailSender SetProtocolLog(string logFilePath)
        {
            _senderOptions.ProtocolLog = logFilePath;
            var sender = Create(_senderOptions, _logger);
            return sender;
        }

        public GenericEmailSender SetPort(ushort smtpPort)
        {
            _senderOptions.SmtpPort = smtpPort;
            return this;
        }

        public GenericEmailSender SetCredential(string username, string password)
        {
            _senderOptions.SmtpCredential = new NetworkCredential(username, password);
            return this;
        }

        public static IProtocolLogger? GetProtocolLogger(string? logFilePath = null, IFileSystem? fileSystem = null)
        {
            IProtocolLogger? protocolLogger = null;
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

        public IGenericEmailWriter WriteEmail => new GenericEmailWriter(this);

        public static MimeMessage ConvertToMimeMessage(IGenericEmail email, CancellationToken cancellationToken = default)
        {
            var mimeMessage = new MimeMessage();

            foreach(var header in email.Headers)
                mimeMessage.Headers.Add(header.Key, header.Value);

            var from = email.From.Select(m => new MailboxAddress(m.Name, m.EmailAddress));
            mimeMessage.From.AddRange(from);

            var replyTo = email.ReplyTo.Select(m => new MailboxAddress(m.Name, m.EmailAddress));
            mimeMessage.ReplyTo.AddRange(replyTo);

            var to = email.To.Select(m => new MailboxAddress(m.Name, m.EmailAddress));
            mimeMessage.To.AddRange(to);

            var cc = email.Cc.Select(m => new MailboxAddress(m.Name, m.EmailAddress));
            mimeMessage.Cc.AddRange(cc);

            var bcc = email.Bcc.Select(m => new MailboxAddress(m.Name, m.EmailAddress));
            mimeMessage.Bcc.AddRange(bcc);

            mimeMessage.Subject = email.Subject ?? string.Empty;

            var bodyBuilder = new BodyBuilder
            {
                TextBody = email.BodyText,
                HtmlBody = email.BodyHtml
            };
            var attachments = email.Attachments
                .Where(a => a.Value is MimeEntity)
                .Select(a => a.Value as MimeEntity);
            foreach (var attachment in attachments)
                bodyBuilder.Attachments.Add(attachment);
            var byteArrays = email.Attachments
                .Where(a => a.Value is byte[]);
            foreach (var byteArray in byteArrays)
                bodyBuilder.Attachments.Add(byteArray.Key, byteArray.Value as byte[]);
            var streams = email.Attachments
                .Where(a => a.Value is Stream);
            foreach (var stream in streams)
                bodyBuilder.Attachments.Add(stream.Key, stream.Value as Stream, cancellationToken);
            var filePaths = email.Attachments
                .Where(a => a.Value is null)
                .Select(a => a.Key);
            foreach (var filePath in filePaths)
                bodyBuilder.Attachments.Add(filePath, cancellationToken);
            mimeMessage.Body = bodyBuilder.ToMessageBody();

            return mimeMessage;
        }

        public async Task SendAsync(IGenericEmail email, CancellationToken cancellationToken = default)
        {
            var mimeMessage = ConvertToMimeMessage(email);
            await _smtpClient.SendAsync(mimeMessage, cancellationToken).ConfigureAwait(false);
        }

        public async Task<bool> TrySendAsync(IGenericEmail email, CancellationToken cancellationToken = default)
        {
            try
            {
                await SendAsync(email, cancellationToken).ConfigureAwait(false);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email.");
                return false;
            }
        }

        public void Dispose()
        {
            _logger.LogTrace("Disposing generic SMTP email client...");
            if (_smtpClient.IsConnected)
                _smtpClient.Disconnect(true);
            _smtpClient.Dispose();
        }
    }
}
