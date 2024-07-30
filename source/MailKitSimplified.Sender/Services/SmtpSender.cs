using MimeKit;
using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using System;
using System.IO;
using System.Net;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MailKitSimplified.Sender.Abstractions;
using MailKitSimplified.Sender.Extensions;
using MailKitSimplified.Sender.Models;
using MailKitSimplified.Sender.Helpers;

namespace MailKitSimplified.Sender.Services
{
    /// <inheritdoc cref="ISmtpSender" />
    public sealed class SmtpSender : ISmtpSender
    {
        private CancellationTokenSource _cts = null;
        private readonly ConcurrentQueue<MimeMessage> _sendQueue = new ConcurrentQueue<MimeMessage>();
        private bool _isClientInjected;
        private ISmtpClient _smtpClient;
        private IProtocolLogger _smtpLogger;
        private ILogger<SmtpSender> _logger;
        private ILoggerFactory _loggerFactory;
        private readonly EmailSenderOptions _senderOptions;

        /// <inheritdoc cref="ISmtpSender" />
        public SmtpSender(IOptions<EmailSenderOptions> senderOptions, ILogger<SmtpSender> logger = null, IProtocolLogger protocolLogger = null, ISmtpClient smtpClient = null, ILoggerFactory loggerFactory = null)
        {
            _loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
            _logger = logger ?? _loggerFactory.CreateLogger<SmtpSender>();
            _senderOptions = senderOptions.Value;
            if (string.IsNullOrWhiteSpace(_senderOptions.SmtpHost))
                throw new ArgumentException($"{nameof(EmailSenderOptions.SmtpHost)} is not set.");
            if (smtpClient == null)
                SetProtocolLog(protocolLogger);
            else
                SetSmtpClient(smtpClient);
        }

        public static SmtpSender Create(string smtpHost, ushort smtpPort = 0, string username = null, string password = null, string protocolLog = null, bool protocolLogFileAppend = false, ILogger<SmtpSender> logger = null, IProtocolLogger protocolLogger = null, ISmtpClient smtpClient = null, ILoggerFactory loggerFactory = null)
        {
            var smtpCredential = new NetworkCredential(username, password);
            var sender = Create(smtpHost, smtpCredential, smtpPort, protocolLog, protocolLogFileAppend, logger, protocolLogger, smtpClient, loggerFactory);
            return sender;
        }

        public static SmtpSender Create(string smtpHost, NetworkCredential smtpCredential, ushort smtpPort = 0, string protocolLog = null, bool protocolLogFileAppend = false, ILogger<SmtpSender> logger = null, IProtocolLogger protocolLogger = null, ISmtpClient smtpClient = null, ILoggerFactory loggerFactory = null)
        {
            var senderOptions = new EmailSenderOptions(smtpHost, smtpCredential, smtpPort, protocolLog, protocolLogFileAppend);
            var sender = Create(senderOptions, logger, protocolLogger, smtpClient, loggerFactory);
            return sender;
        }

        public static SmtpSender Create(EmailSenderOptions emailSenderOptions, ILogger<SmtpSender> logger = null, IProtocolLogger protocolLogger = null, ISmtpClient smtpClient = null, ILoggerFactory loggerFactory = null)
        {
            var senderOptions = Options.Create(emailSenderOptions);
            var sender = new SmtpSender(senderOptions, logger, protocolLogger, smtpClient, loggerFactory);
            return sender;
        }

        public static SmtpSender Create(ISmtpClient smtpClient, EmailSenderOptions emailSenderOptions, ILogger<SmtpSender> logger = null, ILoggerFactory loggerFactory = null)
        {
            var senderOptions = Options.Create(emailSenderOptions);
            var sender = new SmtpSender(senderOptions, logger, null, smtpClient, loggerFactory);
            return sender;
        }

        /// <summary>
        /// Overwrites the existing ISmtpClient and IProtocolLogger,
        /// or creates a new ISmtpClient with an IProtocolLogger if it exists.
        /// </summary>
        public SmtpSender SetSmtpClient(ISmtpClient smtpClient = null)
        {
            if (smtpClient != null)
            {
                _smtpClient = smtpClient;
                _smtpLogger = null;
                _isClientInjected = true;
            }
            else if (_smtpLogger != null)
                _smtpClient = new SmtpClient(_smtpLogger);
            else
                _smtpClient = new SmtpClient();
            return this;
        }

        /// <summary>
        /// Creates a new ISmtpClient with the IProtocolLogger.
        /// </summary>
        public SmtpSender SetProtocolLog(IProtocolLogger protocolLogger)
        {
            _smtpLogger = protocolLogger ?? _senderOptions.CreateProtocolLogger();
            return SetSmtpClient(null);
        }

        /// <summary>
        /// Creates a new IProtocolLogger and ISmtpClient.
        /// </summary>
        public SmtpSender SetProtocolLog(string logFilePath, bool append = false)
        {
            _senderOptions.ProtocolLog = logFilePath;
            _senderOptions.ProtocolLogFileAppend = append;
            return SetProtocolLog(null);
        }

        /// <summary>
        /// Logging setup for those not using dependency injection.
        /// <example>LoggerFactory.Create(_ => _.SetMinimumLevel(LogLevel.Debug).AddDebug().AddConsole());</example>
        /// </summary>
        public SmtpSender SetLogger(ILoggerFactory loggerFactory, ILogger<SmtpSender> logger = null)
        {
            _loggerFactory = loggerFactory ?? _loggerFactory ?? NullLoggerFactory.Instance;
            _logger = logger ?? _logger ?? _loggerFactory.CreateLogger<SmtpSender>();
            return this;
        }

        /// <summary>
        /// Logging setup for those not using dependency injection.
        /// <example>SetLogger(_ => _.SetMinimumLevel(LogLevel.Debug).AddDebug().AddConsole());</example>
        /// </summary>
        public SmtpSender SetLogger(Action<ILoggingBuilder> configure)
        {
            var loggerFactory = LoggerFactory.Create(configure);
            return SetLogger(loggerFactory, null);
        }

        public SmtpSender SetPort(ushort smtpPort, SecureSocketOptions socketOptions = SecureSocketOptions.Auto)
        {
            _senderOptions.SmtpPort = smtpPort;
            _senderOptions.SocketOptions = socketOptions;
            return this;
        }

        public SmtpSender SetCredential(string username, string password)
        {
            _senderOptions.SmtpCredential = new NetworkCredential(username, password);
            return this;
        }

        public SmtpSender RemoveCapabilities(SmtpCapabilities capabilities)
        {
            _senderOptions.CapabilitiesToRemove = capabilities;
            return this;
        }

        public SmtpSender RemoveAuthenticationMechanism(string authenticationMechanismsName)
        {
            if (_smtpClient.AuthenticationMechanisms.Contains(authenticationMechanismsName))
            {
                _smtpClient.AuthenticationMechanisms.Remove(authenticationMechanismsName);
                return SetSmtpClient(_smtpClient);
            }
            return this;
        }

        public SmtpSender SetCustomAuthentication(Func<ISmtpClient, Task> customAuthenticationMethod)
        {
            _senderOptions.CustomAuthenticationMethod = customAuthenticationMethod;
            return this;
        }

        public SmtpSender SetCustomAuthentication(SaslMechanism saslMechanism)
        {
            _senderOptions.AuthenticationMechanism = saslMechanism;
            return this;
        }

        public IEmailWriter WithTemplate(MimeMessage mimeMessageTemplate)
        {
            _logger.LogDebug($"Saved email {mimeMessageTemplate.MessageId} loaded as a template by {this}.");
            return WriteEmail.SetTemplate(mimeMessageTemplate);
        }

        public IEmailWriter WithTemplate(string fileName = EmailWriter.TemplateName)
        {
            if (!File.Exists(fileName))
                return WriteEmail;
            _senderOptions.EmailWriter.TemplateFilePath = fileName;
            var template = MimeMessage.Load(fileName);
            _logger.LogDebug($"Saved email {fileName} loaded as a template by {this}.");
            return WriteEmail.SetTemplate(template);
        }

        public async Task<IEmailWriter> WithTemplateAsync(string fileName = EmailWriter.TemplateName, CancellationToken cancellationToken = default)
        {
            if (!File.Exists(fileName))
                return WriteEmail;
            _senderOptions.EmailWriter.TemplateFilePath = fileName;
            var template = await MimeMessage.LoadAsync(fileName, cancellationToken).ConfigureAwait(false);
            _logger.LogDebug($"Saved email {fileName} loaded as a template by {this}.");
            return WriteEmail.SetTemplate(template);
        }

        public IEmailWriter WriteEmail =>
            new EmailWriter(this, _loggerFactory.CreateLogger<EmailWriter>())
                .SetOptions(_senderOptions.EmailWriter);

        public ISmtpClient SmtpClient => _smtpClient;

        public async ValueTask<ISmtpClient> ConnectSmtpClientAsync(CancellationToken cancellationToken = default) =>
            await GetConnectedAuthenticatedAsync(cancellationToken).ConfigureAwait(false);

        internal async ValueTask<ISmtpClient> GetConnectedAuthenticatedAsync(CancellationToken cancellationToken = default)
        {
            await ConnectAsync(cancellationToken).ConfigureAwait(false);
            await AuthenticateAsync(cancellationToken).ConfigureAwait(false);
            return _smtpClient;
        }

        internal async ValueTask ConnectAsync(CancellationToken cancellationToken = default)
        {
            if (!_smtpClient.IsConnected && !string.IsNullOrEmpty(_senderOptions.SmtpHost))
            {
                await _smtpClient.ConnectAsync(_senderOptions.SmtpHost, _senderOptions.SmtpPort, _senderOptions.SocketOptions, cancellationToken).ConfigureAwait(false);
                _logger.LogTrace($"SMTP client connected to {_senderOptions.SmtpHost}.");
                if (_smtpClient is SmtpClient client && _senderOptions.CapabilitiesToRemove != SmtpCapabilities.None)
                    client.Capabilities &= ~_senderOptions.CapabilitiesToRemove;
                if (_smtpClient.Capabilities.HasFlag(SmtpCapabilities.Size))
                    _logger.LogDebug($"The SMTP server has a size restriction on messages: {_smtpClient.MaxSize}.");
            }
        }

        /// <summary>
        /// Authenticating via a SASL mechanism may be a multi-step process.
        /// <see href="http://www.mimekit.net/docs/html/T_MailKit_Security_SaslMechanism.htm"/>
        /// <seealso href="http://www.mimekit.net/docs/html/T_MailKit_Security_SaslMechanismOAuth2.htm"/>
        /// </summary>
        internal async ValueTask AuthenticateAsync(CancellationToken cancellationToken = default)
        {
            if (_senderOptions.SmtpCredential != null && !_smtpClient.IsAuthenticated)
            {
                if (_senderOptions.CustomAuthenticationMethod != null) // for XOAUTH2 and OAUTHBEARER
                    await _senderOptions.CustomAuthenticationMethod(_smtpClient).ConfigureAwait(false);
                else if (_senderOptions.AuthenticationMechanism != null)
                    await _smtpClient.AuthenticateAsync(_senderOptions.AuthenticationMechanism).ConfigureAwait(false);
                else
                {
                    var ntlm = _smtpClient.AuthenticationMechanisms.Contains("NTLM") ?
                        new SaslMechanismNtlm(_senderOptions.SmtpCredential) : null;
                    if (ntlm?.Workstation != null)
                        await _smtpClient.AuthenticateAsync(ntlm, cancellationToken).ConfigureAwait(false);
                    else
                        await _smtpClient.AuthenticateAsync(_senderOptions.SmtpCredential, cancellationToken).ConfigureAwait(false);
                }
                _logger.LogTrace($"SMTP client authenticated with {_senderOptions.SmtpHost}.");
            }
        }

        public static string ValidateEmailAddresses(IEnumerable<string> sourceEmailAddresses, IEnumerable<string> destinationEmailAddresses)
        {
            if (sourceEmailAddresses is null)
                throw new ArgumentNullException(nameof(sourceEmailAddresses));
            if (destinationEmailAddresses is null)
                throw new ArgumentNullException(nameof(destinationEmailAddresses));
            string warning = null;
            int sourceEmailAddressCount = 0, destinationEmailAddressCount = 0;
            foreach (var from in sourceEmailAddresses)
            {
                if (!from.Contains('@'))
                {
                    warning = $"from address is invalid ({from})";
                }
                foreach (var to in destinationEmailAddresses)
                {
                    if (!to.Contains('@'))
                    {
                        warning = $"to address is invalid ({to})";
                    }
                    if (to.Equals(from, StringComparison.OrdinalIgnoreCase))
                    {
                        warning = $"circular reference, To ({to}) == From ({from})";
                    }
                    destinationEmailAddressCount++;
                }
                sourceEmailAddressCount++;
            }
            if (sourceEmailAddressCount == 0)
                warning = "cource email address not specified";
            else if (destinationEmailAddressCount == 0)
                warning = "destination email address not specified";
            return warning;
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
                var warning = ValidateEmailAddresses(from, toCcBcc);
                isValid = string.IsNullOrEmpty(warning);
                if (!isValid && logger != null)
                    logger.LogWarning($"Email address validation failed for ID {mimeMessage.MessageId}, {warning}.");
            }
            return isValid;
        }

        public static string GetEnvelope(MimeMessage mimeMessage, bool includeTextBody = false)
        {
            string envelope = string.Empty;
            using (var text = new StringWriter())
            {
                text.Write("Message-ID: {0}. ", mimeMessage.MessageId);
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
            if (mimeMessage.ReplyTo.Count == 0 && mimeMessage.From.Count == 0 && _senderOptions.EmailWriter.DefaultReplyToAddress != null)
                mimeMessage.ReplyTo.Add(new MailboxAddress("", _senderOptions.EmailWriter.DefaultReplyToAddress));
            if (mimeMessage.From.Count == 0 && _senderOptions.EmailWriter.GenerateDefaultFromAddress)
                mimeMessage.From.Add(MailboxAddressHelper.GenerateGuidIfFromNotSet(_senderOptions.EmailWriter?.DefaultReplyToAddress));
            _ = ValidateMimeMessage(mimeMessage, _logger);
            _ = await ConnectSmtpClientAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogTrace($"{_senderOptions} sending {GetEnvelope(mimeMessage, includeTextBody: true)}");
            string serverResponse = await _smtpClient.SendAsync(mimeMessage, cancellationToken, transferProgress).ConfigureAwait(false);
            _logger.LogTrace($"{_senderOptions} server response: \"{serverResponse}\".");
            _logger.LogDebug($"{_senderOptions} sent Message-ID {mimeMessage.MessageId}.");
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
                _logger.LogError(ex, $"Failed to authenticate with mail server {_senderOptions}.");
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, $"Failed to connect to mail server {_senderOptions}.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send email through {_senderOptions}. {mimeMessage}");
            }
            return isSent;
        }

        public void Enqueue(MimeMessage mimeMessage)
        {
            if (_cts == null)
                Initialise();
            _sendQueue.Enqueue(mimeMessage);
        }

        private void Initialise()
        {
            _cts = new CancellationTokenSource();
            Task.Run(TrySendAllAsync);
        }

        private async Task TrySendAllAsync()
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                if (_sendQueue.TryDequeue(out MimeMessage mimeMessage))
                {
                    await TrySendAsync(mimeMessage, _cts.Token).ConfigureAwait(false);
                }
                else if (_sendQueue.IsEmpty)
                {
                    await Task.Delay(1000, _cts.Token).ConfigureAwait(false);
                }
            }
        }

        public void ResetSendQueue()
        {
            CancelSendQueue();
            Initialise();
        }

        internal void CancelSendQueue()
        {
            if (_cts != null)
            {
                if (!_sendQueue.IsEmpty)
                    _logger.LogWarning($"Send queue cancelled while messages are still sending");
                _cts.Cancel(false);
#if NET5_0_OR_GREATER
                _sendQueue.Clear();
#endif
            }
        }

        public ISmtpSender Copy() => MemberwiseClone() as ISmtpSender;

        public override string ToString() => _senderOptions.ToString();

        public async Task DisconnectAsync(CancellationToken cancellationToken = default)
        {
            CancelSendQueue();
            _logger.LogTrace("Disconnecting SMTP email client...");
            try
            {
                if (_smtpClient.IsConnected)
                    await _smtpClient.DisconnectAsync(true, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, ex.Message);
            }
        }

        /// <summary>
        /// Disconnect and - if it is not injected - dispose of the SmtpClient.
        /// "Services resolved from the container should never be disposed by the developer."
        /// <see href="https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection-guidelines#disposal-of-services"/>
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            await DisconnectAsync().ConfigureAwait(false);
            if (!_isClientInjected)
                _smtpClient.Dispose();
        }

        /// <summary>
        /// Disconnect and - if it is not injected - dispose of the SmtpClient.
        /// "Services resolved from the container should never be disposed by the developer."
        /// <see href="https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection-guidelines#disposal-of-services"/>
        /// </summary>
        public void Dispose()
        {
            if (_cts != null)
                TrySendAllAsync().GetAwaiter().GetResult();
            DisconnectAsync().GetAwaiter().GetResult();
            if (!_isClientInjected)
                _smtpClient.Dispose();
        }
    }
}