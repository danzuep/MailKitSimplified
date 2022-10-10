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
using MailKitSimplified.Core.Services;
using MailKitSimplified.Sender.Abstractions;

namespace MailKitSimplified.Sender.Services
{
    public class MimeMessageSender : IMimeMessageSender
    {
        private readonly EmailSenderOptions _senderOptions;
        private readonly IProtocolLogger _smtpLogger;
        private readonly SmtpClient _smtpClient;
        private readonly ILogger _logger;
        private readonly IMimeAttachmentHandler _attachmentHandler;

        public MimeMessageSender(IOptions<EmailSenderOptions> senderOptions, ILoggerFactory loggerFactory = null, IMimeAttachmentHandler mimeAttachmentHandler = null)
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
            _logger = loggerFactory?.CreateLogger<MimeMessageSender>() ?? NullLogger<MimeMessageSender>.Instance;
            _attachmentHandler = mimeAttachmentHandler ?? new MimeAttachmentHandler(loggerFactory?.CreateLogger<MimeAttachmentHandler>(), new FileHandler(loggerFactory?.CreateLogger<FileHandler>()));
        }

        public static MimeMessageSender Create(string smtpHost, int smtpPort = 0, string userName = null, string password = null, string protocolLog = null)
        {
            var smtpCredential = userName == null && password == null ? null : new NetworkCredential(userName ?? "", password ?? "");
            var senderOptions = new EmailSenderOptions(smtpHost, smtpPort, smtpCredential, protocolLog);
            var options = Options.Create(senderOptions);
            var sender = new MimeMessageSender(options);
            return sender;
        }

        public MimeMessageWriter MimeEmail => MimeMessageWriter.CreateFrom(this);

        public IEmailWriter WriteEmail => Email.Write(this);

        public static async Task<MimeMessage> ConvertToMimeMessage(IEmail email, IMimeAttachmentHandler attachmentHandler, CancellationToken cancellationToken = default)
        {
            var mimeMessage = new MimeMessage();

            var from = email.From.Select(m => new MailboxAddress(m.Name, m.Address));
            mimeMessage.From.AddRange(from);
            mimeMessage.ReplyTo.AddRange(from);

            var to = email.To.Select(m => new MailboxAddress(m.Name, m.Address));
            mimeMessage.To.AddRange(to);

            mimeMessage.Subject = email.Subject ?? string.Empty;

            mimeMessage.Body = new TextPart(TextFormat.Html) { Text = email.Body ?? string.Empty };

            await attachmentHandler.AddAttachments(mimeMessage, email.AttachmentFilePaths, cancellationToken).ConfigureAwait(false);

            return mimeMessage;
        }

        private void ValidateMimeMessage(MimeMessage mimeMessage)
        {
            if (mimeMessage is null)
                throw new ArgumentNullException(nameof(mimeMessage));
            if (mimeMessage.From.Count == 0)
                throw new MissingMemberException(nameof(MimeMessage), nameof(MimeMessage.From));
            if (mimeMessage.To.Count == 0)
                throw new MissingMemberException(nameof(MimeMessage), nameof(MimeMessage.To));
            var from = mimeMessage.From.Mailboxes.Select(m => m.Address);
            var to = mimeMessage.To.Mailboxes.Select(m => m.Address);
            Email.ValidateEmailAddresses(from, to, _logger);
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
            await _attachmentHandler.AddAttachments(mimeMessage, attachmentFilePaths, cancellationToken).ConfigureAwait(false);
            await SendAsync(mimeMessage, cancellationToken).ConfigureAwait(false);
        }

        public async Task SendAsync(MimeMessage mimeMessage, CancellationToken cancellationToken = default)
        {
            ValidateMimeMessage(mimeMessage);
            await ConnectSmtpClient(cancellationToken).ConfigureAwait(false);
            Debug.WriteLine("Sending to: {0}, subject: '{1}'", mimeMessage.To, mimeMessage.Subject);
            string serverResponse = await _smtpClient.SendAsync(mimeMessage, cancellationToken).ConfigureAwait(false);
            Debug.WriteLine(serverResponse);
        }

        public async Task SendAsync(IEmail email, CancellationToken cancellationToken = default)
        {
            var mimeMessage = await ConvertToMimeMessage(email, _attachmentHandler, cancellationToken).ConfigureAwait(false);
            await ConnectSmtpClient(cancellationToken).ConfigureAwait(false);
            Debug.WriteLine("Sending email {0}", email.ToString());
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
            _smtpLogger?.Dispose();
        }
    }
}