using MimeKit;
using MailKit.Net.Smtp;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MailKitSimplified.Generic.Abstractions;
using MailKitSimplified.Generic.Services;
using MailKitSimplified.Generic.Models;

namespace MailKitSimplified.Email.Services
{
    internal class GenericEmailSender : IGenericEmailSender
    {
        private readonly ILogger<GenericEmailSender> _logger;
        private readonly ISmtpClient _smtpClient;
        private readonly GenericSmtpOptions _senderOptions;

        public GenericEmailSender(IOptions<GenericSmtpOptions> senderOptions, ILogger<GenericEmailSender> logger = null, ISmtpClient smtpClient = null)
        {
            _logger = logger ?? NullLogger<GenericEmailSender>.Instance;
            _senderOptions = senderOptions.Value;
            if (string.IsNullOrWhiteSpace(_senderOptions.Host))
                throw new ArgumentException($"{nameof(GenericSmtpOptions.Host)} is not set.");
            _smtpClient = smtpClient ?? new SmtpClient();
        }

        public static GenericEmailSender Create(string smtpHost, ILogger<GenericEmailSender> logger = null)
        {
            if (string.IsNullOrWhiteSpace(smtpHost))
                throw new ArgumentNullException(nameof(smtpHost));
            ushort smtpPort = 0;
            string[] hostParts = smtpHost.Split(':');
            if (hostParts.Length == 2 && ushort.TryParse(hostParts[1], out smtpPort))
                smtpHost = hostParts[0];
            var emailSenderOptions = new GenericSmtpOptions
            {
                Host = smtpHost,
                Port = smtpPort
            };
            var senderOptions = Options.Create(emailSenderOptions);
            var sender = new GenericEmailSender(senderOptions, logger);
            return sender;
        }

        public GenericEmailSender SetPort(ushort smtpPort)
        {
            _senderOptions.Port = smtpPort;
            return this;
        }

        public GenericEmailSender SetCredential(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username))
                throw new ArgumentNullException(nameof(username));
            _senderOptions.Username = username;
            _senderOptions.Password = password;
            return this;
        }

        public IGenericEmailWriter WriteEmail => new GenericEmailWriter(this);

        public static MimeMessage ConvertToMimeMessage(IGenericEmail email, CancellationToken cancellationToken = default)
        {
            var mimeMessage = new MimeMessage();

            foreach (var header in email.Headers)
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
                _logger.LogError(ex, $"Failed to send email: {email}");
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
