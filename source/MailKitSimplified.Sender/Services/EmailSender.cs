using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MimeKit;
using MimeKit.Text;
using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using MailKitSimplified.Sender.Abstractions;
using MailKitSimplified.Sender.Models;

namespace MailKitSimplified.Sender.Services
{
    public class EmailSender : IEmailSender
    {
        private readonly EmailSenderOptions _senderOptions;
        private readonly IProtocolLogger _smtpLogger;
        private readonly SmtpClient _smtpClient;
        private readonly ILogger _logger = NullLogger.Instance;

        public EmailSender(IOptions<EmailSenderOptions> senderOptions, ILogger<EmailSender> logger = null)
        {
            _senderOptions = senderOptions?.Value ?? throw new ArgumentException(nameof(senderOptions));
            _smtpLogger = _senderOptions.ProtocolLog == null ? null :
                string.IsNullOrEmpty(_senderOptions.ProtocolLog) ?
                    new ProtocolLogger(Console.OpenStandardError()) :
                        new ProtocolLogger(_senderOptions.ProtocolLog);
            _smtpClient = _smtpLogger != null ? new SmtpClient(_smtpLogger) : new SmtpClient();
            if (logger != null) _logger = logger;
        }

        public static IEmailSender Create(string smtpHost)
        {
            var senderOptions = new EmailSenderOptions { SmtpHost = smtpHost };
            var options = Options.Create(senderOptions);
            return new EmailSender(options, NullLogger<EmailSender>.Instance);
        }

        public IFluentEmail Email => new FluentEmail(this);

        public IEmail CreateEmail => new Email(this);

        public IEmail WriteEmail(string fromAddress, string toAddress, string subject = "", string body = "", bool isHtml = true, params string[] attachmentFilePaths)
        {
            return CreateEmail.Write(fromAddress, toAddress, subject, body, isHtml, attachmentFilePaths);
        }

        public async Task<bool> CheckConnection(CancellationToken cancellationToken = default)
        {
            bool isHealthy = false;
            try
            {
                if (!_smtpClient.IsConnected && !string.IsNullOrEmpty(_senderOptions.SmtpHost))
                    await _smtpClient.ConnectAsync(_senderOptions.SmtpHost, cancellationToken: cancellationToken);
                if (_senderOptions.SmtpCredential != null && _senderOptions.SmtpCredential != default && !_smtpClient.IsAuthenticated)
                    await _smtpClient.AuthenticateAsync(_senderOptions.SmtpCredential, cancellationToken);
            }
            catch (AuthenticationException ex)
            {
                _logger.LogError(ex, "Failed to authenticate with mail server.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect to mail server.");
            }
            return isHealthy;
        }

        private static bool HasCircularReference(IEmail email)
        {
            bool isCircular = false;
            if (email != null && email.From != null && email.To != null)
                isCircular = email.To.Any(t => t.Address.Equals(email.From.Address, StringComparison.OrdinalIgnoreCase));
            return isCircular;
        }

        public static async Task<MimeMessage> ToMimeMessage(IEmail email, MimeEntityConverter mimeEntityConverter)
        {
            var mimeMessage = new MimeMessage();

            var from = new MailboxAddress(email.From.Name, email.From.Address);
            mimeMessage.From.Add(from);
            mimeMessage.ReplyTo.Add(from);

            var to = email.To.Select(t => new MailboxAddress(t.Name, t.Address));
            mimeMessage.To.AddRange(to);

            mimeMessage.Subject = email.Subject ?? string.Empty;

            MimeEntity body = new TextPart(TextFormat.Html) { Text = email.Body ?? string.Empty };

            if (email.AttachmentFilePaths?.Count > 0)
            {
                var multipart = new Multipart();
                multipart.Add(body);
                var attachmentFilePaths = email.AttachmentFilePaths.ToArray();
                var mimeParts = await mimeEntityConverter.GetMimeEntitiesFromFilePathsAsync(attachmentFilePaths);
                foreach (var mimePart in mimeParts)
                    multipart.Add(mimePart);
                body = multipart;
            }

            mimeMessage.Body = body;

            return mimeMessage;
        }

        public async Task<bool> SendAsync(IEmail email, CancellationToken cancellationToken = default)
        {
            if (email is null)
                throw new ArgumentNullException(nameof(email));
            if (email.To.Count == 0)
                throw new MissingMemberException(nameof(IEmail), nameof(IEmail.To));
            if (HasCircularReference(email))
                _logger.LogWarning("Circular reference, ToEmailAddress == FromEmailAddress");
            bool isSent = false;
            if (await CheckConnection())
            {
                Debug.WriteLine("Sending email {0}", email);
                var mimeMessage = await ToMimeMessage(email, new MimeEntityConverter());
                string serverResponse = await _smtpClient.SendAsync(mimeMessage, cancellationToken);
                Debug.WriteLine(serverResponse);
                isSent = true;
            }
            return isSent;
        }

        public async Task<bool> SendAsync(MimeMessage mimeMessage, CancellationToken cancellationToken = default)
        {
            bool isSent = false;
            if (await CheckConnection())
            {
                Debug.WriteLine("Sending to: {0}, subject: '{1}'", mimeMessage.To, mimeMessage.Subject);
                string serverResponse = await _smtpClient.SendAsync(mimeMessage, cancellationToken);
                Debug.WriteLine(serverResponse);
                isSent = true;
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