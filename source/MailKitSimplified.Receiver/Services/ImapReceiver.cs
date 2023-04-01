using MailKit;
using MailKit.Net.Imap;
using MailKit.Security;
using System;
using System.Net;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging.Abstractions;
using MailKitSimplified.Receiver.Abstractions;
using MailKitSimplified.Receiver.Models;
using MailKitSimplified.Receiver.Extensions;

namespace MailKitSimplified.Receiver.Services
{
    public sealed class ImapReceiver : IImapReceiver
    {
        private Func<IImapClient, Task> _customAuthenticationMethod;
        private Lazy<MailFolderClient> _mailFolderClient;
        private Lazy<MailFolderReader> _mailFolderReader;
        private Lazy<MailFolderMonitor> _mailFolderMonitor;

        private readonly ILogger<ImapReceiver> _logger;
        private readonly ILoggerFactory _loggerFactory;
        private readonly IImapClient _imapClient;
        private readonly EmailReceiverOptions _receiverOptions;

        public ImapReceiver(IOptions<EmailReceiverOptions> receiverOptions, ILogger<ImapReceiver> logger = null, IProtocolLogger protocolLogger = null, IImapClient imapClient = null, ILoggerFactory loggerFactory = null)
        {
            _logger = logger ?? NullLogger<ImapReceiver>.Instance;
            _loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
            _receiverOptions = receiverOptions.Value;
            if (string.IsNullOrWhiteSpace(_receiverOptions.ImapHost))
                throw new NullReferenceException($"{nameof(EmailReceiverOptions.ImapHost)} is null.");
            if (_receiverOptions.ImapCredential == null)
                throw new NullReferenceException($"{nameof(EmailReceiverOptions.ImapCredential)} is null.");
            var imapLogger = protocolLogger ?? MailKitProtocolLogger.Create(_receiverOptions.ProtocolLogger, _loggerFactory.CreateLogger<MailKitProtocolLogger>());
            _mailFolderClient = new Lazy<MailFolderClient>(() => Services.MailFolderClient.Create(_receiverOptions, _loggerFactory.CreateLogger<MailFolderClient>(), _loggerFactory.CreateLogger<ImapReceiver>()));
            _mailFolderReader = new Lazy<MailFolderReader>(() => MailFolderReader.Create(_receiverOptions, _loggerFactory.CreateLogger<MailFolderReader>(), _loggerFactory.CreateLogger<ImapReceiver>()));
            _mailFolderMonitor = new Lazy<MailFolderMonitor>(() => MailFolderMonitor.Create(_receiverOptions, _loggerFactory.CreateLogger<MailFolderMonitor>(), _loggerFactory.CreateLogger<ImapReceiver>()));
            _imapClient = imapClient ?? new ImapClient(imapLogger);
        }

        public static ImapReceiver Create(string imapHost, ushort imapPort = 0, string username = null, string password = null, string mailFolderName = null, string protocolLog = null, bool protocolLogFileAppend = false)
        {
            var imapCredential = new NetworkCredential(username, password);
            var receiver = Create(imapHost, imapCredential, imapPort, mailFolderName, protocolLog, protocolLogFileAppend);
            return receiver;
        }

        public static ImapReceiver Create(string imapHost, NetworkCredential imapCredential, ushort imapPort = 0, string mailFolderName = null, string protocolLog = null, bool protocolLogFileAppend = false)
        {
            var receiverOptions = new EmailReceiverOptions(imapHost, imapCredential, imapPort, mailFolderName, protocolLog, protocolLogFileAppend);
            var receiver = Create(receiverOptions);
            return receiver;
        }

        public static ImapReceiver Create(EmailReceiverOptions emailReceiverOptions, ILogger<ImapReceiver> logger = null)
        {
            if (emailReceiverOptions == null)
                throw new ArgumentNullException(nameof(emailReceiverOptions));
            var options = Options.Create(emailReceiverOptions);
            var receiver = new ImapReceiver(options, logger);
            return receiver;
        }

        public ImapReceiver SetProtocolLog(string logFilePath, bool append = false)
        {
            _receiverOptions.ProtocolLogger.FileWriter.FilePath = logFilePath;
            _receiverOptions.ProtocolLogger.FileWriter.AppendToExisting = append;
            var receiver = Create(_receiverOptions, _logger);
            return receiver;
        }

        public ImapReceiver SetPort(ushort imapPort)
        {
            _receiverOptions.ImapPort = imapPort;
            return this;
        }

        public ImapReceiver SetCredential(string username, string password)
        {
            _receiverOptions.ImapCredential = new NetworkCredential(username, password);
            return this;
        }

        public ImapReceiver SetFolder(string mailFolderName)
        {
            _receiverOptions.MailFolderName = mailFolderName;
            return this;
        }

        public ImapReceiver SetCustomAuthentication(Func<IImapClient, Task> customAuthenticationMethod)
        {
            _customAuthenticationMethod = customAuthenticationMethod;
            return this;
        }

        public IMailFolderClient GetFolder(string mailFolderName)
        {
            _receiverOptions.MailFolderName = mailFolderName;
            return MailFolderClient;
        }

        public IMailFolderReader ReadFrom(string mailFolderName)
        {
            _receiverOptions.MailFolderName = mailFolderName;
            return ReadMail;
        }

        public IMailFolderMonitor Monitor(string mailFolderName)
        {
            _receiverOptions.MailFolderName = mailFolderName;
            return MonitorFolder;
        }

        public IMailFolderClient MailFolderClient => _mailFolderClient.Value;

        public IMailFolderReader ReadMail => _mailFolderReader.Value;

        public IMailFolderMonitor MonitorFolder => _mailFolderMonitor.Value;

        public async ValueTask<IImapClient> ConnectAuthenticatedImapClientAsync(CancellationToken cancellationToken = default)
        {
            await ConnectImapClientAsync(cancellationToken).ConfigureAwait(false);
            await AuthenticateImapClientAsync(cancellationToken).ConfigureAwait(false);
            return _imapClient;
        }

        internal async ValueTask ConnectImapClientAsync(CancellationToken cancellationToken = default)
        {
            if (!_imapClient.IsConnected)
            {
                await _imapClient.ConnectAsync(_receiverOptions.ImapHost, _receiverOptions.ImapPort, SecureSocketOptions.Auto, cancellationToken).ConfigureAwait(false);
                if (_imapClient.Capabilities.HasFlag(ImapCapabilities.Compress))
                    await _imapClient.CompressAsync(cancellationToken).ConfigureAwait(false);
                _logger.LogTrace($"IMAP client connected to {_receiverOptions.ImapHost}.");
            }
        }

        /// <summary>
        /// Authenticating via a SASL mechanism may be a multi-step process.
        /// <see href="http://www.mimekit.net/docs/html/T_MailKit_Security_SaslMechanism.htm"/>
        /// <seealso href="http://www.mimekit.net/docs/html/T_MailKit_Security_SaslMechanismOAuth2.htm"/>
        /// </summary>
        internal async ValueTask AuthenticateImapClientAsync(CancellationToken cancellationToken = default)
        {
            if (!_imapClient.IsAuthenticated)
            {
                if (_customAuthenticationMethod != null) // for XOAUTH2 and OAUTHBEARER
                    await _customAuthenticationMethod(_imapClient).ConfigureAwait(false);
                else
                {
                    var ntlm = _imapClient.AuthenticationMechanisms.Contains("NTLM") ?
                        new SaslMechanismNtlm(_receiverOptions.ImapCredential) : null;
                    if (ntlm?.Workstation != null)
                        await _imapClient.AuthenticateAsync(ntlm, cancellationToken).ConfigureAwait(false);
                    else
                        await _imapClient.AuthenticateAsync(_receiverOptions.ImapCredential, cancellationToken).ConfigureAwait(false);
                }
                _logger.LogTrace($"IMAP client authenticated with {_receiverOptions.ImapHost}.");
            }
        }

        public void RemoveAuthenticationMechanism(string authenticationMechanismsName)
        {
            if (_imapClient.AuthenticationMechanisms.Contains(authenticationMechanismsName))
                _imapClient.AuthenticationMechanisms.Remove(authenticationMechanismsName);
        }

        /// <exception cref="FolderNotFoundException">No mail folder has the specified name</exception>
        public async ValueTask<IMailFolder> ConnectMailFolderAsync(CancellationToken cancellationToken = default)
        {
            _ = await ConnectAuthenticatedImapClientAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogTrace($"Connecting to mail folder: '{_receiverOptions.MailFolderName}'.");
            var mailFolder = string.IsNullOrWhiteSpace(_receiverOptions.MailFolderName) || _receiverOptions.MailFolderName.Equals("INBOX", StringComparison.OrdinalIgnoreCase) ?
                _imapClient.Inbox : await _imapClient.GetFolderAsync(_receiverOptions.MailFolderName, cancellationToken).ConfigureAwait(false);
            return mailFolder;
        }

        public async Task<IList<string>> GetMailFolderNamesAsync(CancellationToken cancellationToken = default)
        {
            _ = await ConnectAuthenticatedImapClientAsync(cancellationToken).ConfigureAwait(false);
            var mailFolderNames = new List<string>();
            if (_imapClient.PersonalNamespaces.Count > 0)
            {
                var rootFolder = await _imapClient.GetFoldersAsync(_imapClient.PersonalNamespaces[0], cancellationToken: cancellationToken).ConfigureAwait(false);
                var subfolders = rootFolder.SelectMany(rf => rf.GetSubfolders().Select(sf => sf.Name));
                var inboxSubfolders = _imapClient.Inbox.GetSubfolders(cancellationToken: cancellationToken).Select(f => f.FullName);
                mailFolderNames.AddRange(inboxSubfolders);
                mailFolderNames.AddRange(subfolders);
                _logger.LogDebug($"{inboxSubfolders.Count()} Inbox folders: {inboxSubfolders.ToEnumeratedString()}.");
                _logger.LogDebug($"{subfolders.Count()} personal folders: {subfolders.ToEnumeratedString()}.");
            }
            if (_imapClient.SharedNamespaces.Count > 0)
            {
                var rootFolder = await _imapClient.GetFoldersAsync(_imapClient.SharedNamespaces[0], cancellationToken: cancellationToken).ConfigureAwait(false);
                var subfolders = rootFolder.SelectMany(rf => rf.GetSubfolders(cancellationToken: cancellationToken).Select(sf => sf.Name));
                mailFolderNames.AddRange(subfolders);
                _logger.LogDebug($"{subfolders.Count()} shared folders: {subfolders.ToEnumeratedString()}.");
            }
            if (_imapClient.OtherNamespaces.Count > 0)
            {
                var rootFolder = await _imapClient.GetFoldersAsync(_imapClient.OtherNamespaces[0], cancellationToken: cancellationToken).ConfigureAwait(false);
                var subfolders = rootFolder.SelectMany(rf => rf.GetSubfolders(cancellationToken: cancellationToken).Select(sf => sf.Name));
                mailFolderNames.AddRange(subfolders);
                _logger.LogDebug($"{subfolders.Count()} other folders: {subfolders.ToEnumeratedString()}.");
            }
            return mailFolderNames;
        }

        public IImapReceiver Clone()
        {
            var receiverOptions = _receiverOptions.Copy();
            return Create(receiverOptions, _logger);
        }

        public override string ToString() => _receiverOptions.ToString();

        public async Task DisconnectAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogTrace("Disconnecting IMAP email client...");
            if (_mailFolderClient.IsValueCreated)
                _mailFolderClient.Value.Dispose();
            if (_mailFolderReader.IsValueCreated)
                _mailFolderReader.Value.Dispose();
            if (_imapClient.IsConnected)
                await _imapClient.DisconnectAsync(true, cancellationToken).ConfigureAwait(false);
        }

        public async ValueTask DisposeAsync()
        {
            await DisconnectAsync().ConfigureAwait(false);
            _imapClient.Dispose();
        }

        public void Dispose()
        {
            DisconnectAsync().GetAwaiter().GetResult();
            _imapClient.Dispose();
            _loggerFactory.Dispose();
        }
    }
}
