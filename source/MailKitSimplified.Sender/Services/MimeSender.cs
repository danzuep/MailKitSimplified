using System;
using System.Net;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using MimeKit;
using MailKit;
using MailKit.Net.Smtp;
using MailKitSimplified.Sender.Abstractions;
using MailKitSimplified.Sender.Models;

namespace MailKitSimplified.Sender
{
    public class MimeSender : IMimeEmailSender
    {
        private readonly EmailSenderOptions _senderOptions;
        private readonly IProtocolLogger _smtpLogger;
        private readonly SmtpClient _smtpClient;

        public MimeSender(IOptions<EmailSenderOptions> senderOptions)
        {
            _senderOptions = senderOptions?.Value ?? throw new ArgumentException(nameof(senderOptions));
            if (string.IsNullOrWhiteSpace(_senderOptions.SmtpHost))
                throw new NullReferenceException(nameof(_senderOptions.SmtpHost));
            _smtpLogger = _senderOptions.ProtocolLog == null ? null :
                string.IsNullOrEmpty(_senderOptions.ProtocolLog) ?
                    new ProtocolLogger(Console.OpenStandardError()) :
                        new ProtocolLogger(_senderOptions.ProtocolLog);
            _smtpClient = _smtpLogger != null ? new SmtpClient(_smtpLogger) : new SmtpClient();
        }

        public static IMimeEmailSender Create(string smtpHost, NetworkCredential smtpCredential = null, string protocolLog = null)
        {
            if (string.IsNullOrWhiteSpace(smtpHost))
                throw new ArgumentNullException(nameof(smtpHost));
            var senderOptions = new EmailSenderOptions
            {
                SmtpHost = smtpHost,
                SmtpCredential = smtpCredential,
                ProtocolLog = protocolLog
            };
            var options = Options.Create(senderOptions);
            return new MimeSender(options);
        }

        public IFluentEmail Email => new MimeEmail(this);

        public async Task ConnectSmtpClient(CancellationToken cancellationToken = default)
        {
            if (!_smtpClient.IsConnected && !string.IsNullOrEmpty(_senderOptions.SmtpHost))
                await _smtpClient.ConnectAsync(_senderOptions.SmtpHost, cancellationToken: cancellationToken);
            if (_senderOptions.SmtpCredential != null && _senderOptions.SmtpCredential != default && !_smtpClient.IsAuthenticated)
                await _smtpClient.AuthenticateAsync(_senderOptions.SmtpCredential, cancellationToken);
        }

        public async Task SendAsync(MimeMessage mimeMessage, CancellationToken cancellationToken = default)
        {
            await ConnectSmtpClient(cancellationToken);
            Debug.WriteLine("Sending to: {0}, subject: '{1}'", mimeMessage.To, mimeMessage.Subject);
            string serverResponse = await _smtpClient.SendAsync(mimeMessage, cancellationToken);
            Debug.WriteLine(serverResponse);
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