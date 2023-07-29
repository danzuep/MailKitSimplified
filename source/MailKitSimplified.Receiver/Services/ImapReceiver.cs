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
        private Lazy<MailFolderReader> _mailFolderReader;
        private Lazy<MailFolderMonitor> _mailFolderMonitor;
        private Func<IImapClient, Task> _customAuthenticationMethod;
        private IImapClient _imapClient;
        private IProtocolLogger _imapLogger;
        private ILogger<ImapReceiver> _logger;
        private ILoggerFactory _loggerFactory;
        private EmailReceiverOptions _receiverOptions;

        public ImapReceiver(IOptions<EmailReceiverOptions> receiverOptions, ILogger<ImapReceiver> logger = null, IProtocolLogger protocolLogger = null, IImapClient imapClient = null, ILoggerFactory loggerFactory = null)
        {
            _loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
            _logger = logger ?? _loggerFactory.CreateLogger<ImapReceiver>();
            SetOptions(receiverOptions?.Value);
            if (imapClient == null)
                SetProtocolLog(protocolLogger);
            else
                SetImapClient(imapClient);
        }

        public static ImapReceiver Create(string imapHost, ushort imapPort = 0, string username = null, string password = null, string mailFolderName = null, string protocolLog = null, bool protocolLogFileAppend = false, ILogger<ImapReceiver> logger = null, IProtocolLogger protocolLogger = null, IImapClient imapClient = null, ILoggerFactory loggerFactory = null)
        {
            var imapCredential = new NetworkCredential(username, password);
            var receiver = Create(imapHost, imapCredential, imapPort, mailFolderName, protocolLog, protocolLogFileAppend, logger, protocolLogger, imapClient, loggerFactory);
            return receiver;
        }

        public static ImapReceiver Create(string imapHost, NetworkCredential imapCredential, ushort imapPort = 0, string mailFolderName = null, string protocolLog = null, bool protocolLogFileAppend = false, ILogger<ImapReceiver> logger = null, IProtocolLogger protocolLogger = null, IImapClient imapClient = null, ILoggerFactory loggerFactory = null)
        {
            var receiverOptions = new EmailReceiverOptions(imapHost, imapCredential, imapPort, mailFolderName, protocolLog, protocolLogFileAppend);
            var receiver = Create(receiverOptions, logger, protocolLogger, imapClient, loggerFactory);
            return receiver;
        }

        public static ImapReceiver Create(EmailReceiverOptions emailReceiverOptions, ILogger<ImapReceiver> logger = null, IProtocolLogger protocolLogger = null, IImapClient imapClient = null, ILoggerFactory loggerFactory = null)
        {
            if (emailReceiverOptions == null)
                throw new ArgumentNullException(nameof(emailReceiverOptions));
            var options = Options.Create(emailReceiverOptions);
            var receiver = new ImapReceiver(options, logger, protocolLogger, imapClient, loggerFactory);
            return receiver;
        }

        public static ImapReceiver Create(IImapClient imapClient, EmailReceiverOptions emailReceiverOptions, ILogger<ImapReceiver> logger = null, ILoggerFactory loggerFactory = null)
        {
            if (emailReceiverOptions == null)
                throw new ArgumentNullException(nameof(emailReceiverOptions));
            var options = Options.Create(emailReceiverOptions);
            var receiver = new ImapReceiver(options, logger, null, imapClient, loggerFactory);
            return receiver;
        }

        private ImapReceiver SetOptions(EmailReceiverOptions emailReceiverOptions)
        {
            if (emailReceiverOptions == null)
                throw new ArgumentNullException(nameof(emailReceiverOptions));
            _receiverOptions = emailReceiverOptions;
            if (string.IsNullOrWhiteSpace(_receiverOptions.ImapHost))
                throw new ArgumentException($"{nameof(EmailReceiverOptions.ImapHost)} is not set.");
            if (_receiverOptions.ImapCredential == null)
                throw new ArgumentException($"{nameof(EmailReceiverOptions.ImapCredential)} is null.");
            _mailFolderReader = new Lazy<MailFolderReader>(() => MailFolderReader.Create(
                _imapClient, _receiverOptions, _loggerFactory.CreateLogger<MailFolderReader>(), _loggerFactory.CreateLogger<ImapReceiver>()));
            _mailFolderMonitor = new Lazy<MailFolderMonitor>(() => MailFolderMonitor.Create(
                _receiverOptions, _loggerFactory.CreateLogger<MailFolderMonitor>(), _loggerFactory.CreateLogger<ImapReceiver>(), _imapLogger));
            return this;
        }

        /// <summary>
        /// Overwrites the existing IImapClient and IProtocolLogger,
        /// or creates a new IImapClient with an IProtocolLogger if it exists.
        /// </summary>
        public ImapReceiver SetImapClient(IImapClient imapClient = null)
        {
            if (imapClient != null)
            {
                _imapClient = imapClient;
                _imapLogger = null;
            }
            else if (_imapLogger != null)
                _imapClient = new ImapClient(_imapLogger);
            else
                _imapClient = new ImapClient();
            _mailFolderReader = new Lazy<MailFolderReader>(() => MailFolderReader.Create(
                _imapClient, _receiverOptions, _loggerFactory.CreateLogger<MailFolderReader>(), _loggerFactory.CreateLogger<ImapReceiver>()));
            return this;
        }

        /// <summary>
        /// Creates a new IImapClient with the IProtocolLogger.
        /// </summary>
        public ImapReceiver SetProtocolLog(IProtocolLogger protocolLogger)
        {
            _imapLogger = protocolLogger ?? new MailKitProtocolLogger(
                new LogFileWriter(_loggerFactory.CreateLogger<LogFileWriter>()),
                Options.Create(_receiverOptions.ProtocolLogger),
                _loggerFactory.CreateLogger<MailKitProtocolLogger>());
            _mailFolderMonitor = new Lazy<MailFolderMonitor>(() => MailFolderMonitor.Create(
                _receiverOptions, _loggerFactory.CreateLogger<MailFolderMonitor>(), _loggerFactory.CreateLogger<ImapReceiver>(), _imapLogger));
            return SetImapClient(null);
        }

        /// <summary>
        /// Creates a new IProtocolLogger and IImapClient.
        /// </summary>
        public ImapReceiver SetProtocolLog(string logFilePath, bool append = false)
        {
            _receiverOptions.ProtocolLogger.FileWriter.FilePath = logFilePath;
            _receiverOptions.ProtocolLogger.FileWriter.AppendToExisting = append;
            _imapLogger = _receiverOptions.ProtocolLogger.CreateProtocolLogger();
            return SetProtocolLog(_imapLogger);
        }

        public ImapReceiver SetPort(ushort imapPort, SecureSocketOptions socketOptions = SecureSocketOptions.Auto)
        {
            _receiverOptions.ImapPort = imapPort;
            _receiverOptions.SocketOptions = socketOptions;
            return SetOptions(_receiverOptions);
        }

        public ImapReceiver SetCredential(string username, string password)
        {
            _receiverOptions.ImapCredential = new NetworkCredential(username, password);
            return SetOptions(_receiverOptions);
        }

        public ImapReceiver SetFolder(string mailFolderName, FolderAccess folderAccess = FolderAccess.None)
        {
            _receiverOptions.MailFolderName = mailFolderName;
            _receiverOptions.MailFolderAccess = folderAccess;
            return SetOptions(_receiverOptions);
        }

        /// <summary>
        /// For those not using dependency injection.
        /// <example>LoggerFactory.Create(_ => _.SetMinimumLevel(LogLevel.Debug).AddDebug().AddConsole());</example>
        /// </summary>
        public ImapReceiver SetLogger(ILoggerFactory loggerFactory, ILogger<ImapReceiver> logger = null)
        {
            _loggerFactory = loggerFactory ?? _loggerFactory ?? NullLoggerFactory.Instance;
            _logger = logger ?? _logger ?? _loggerFactory.CreateLogger<ImapReceiver>();
            return SetOptions(_receiverOptions);
        }

        public ImapReceiver RemoveCapabilities(ImapCapabilities capabilities)
        {
            _receiverOptions.CapabilitiesToRemove = capabilities;
            return this;
        }

        public ImapReceiver RemoveAuthenticationMechanism(string authenticationMechanismsName)
        {
            if (_imapClient.AuthenticationMechanisms.Contains(authenticationMechanismsName))
            {
                _imapClient.AuthenticationMechanisms.Remove(authenticationMechanismsName);
                return SetImapClient(_imapClient);
            }
            return this;
        }

        public ImapReceiver SetCustomAuthentication(Func<IImapClient, Task> customAuthenticationMethod)
        {
            _customAuthenticationMethod = customAuthenticationMethod;
            return this;
        }

        [Obsolete("Use ReadFrom instead.")]
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

        [Obsolete("Use ReadMail instead.")]
        public IMailFolderClient MailFolderClient => Services.MailFolderClient.Create(_imapClient, _receiverOptions, _loggerFactory.CreateLogger<MailFolderClient>(), _loggerFactory.CreateLogger<ImapReceiver>());

        public IMailFolderReader ReadMail => _mailFolderReader.Value;

        public IMailFolderMonitor MonitorFolder => _mailFolderMonitor.Value;

        public IImapClient ImapClient => _imapClient;

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
                await _imapClient.ConnectAsync(_receiverOptions.ImapHost, _receiverOptions.ImapPort, _receiverOptions.SocketOptions, cancellationToken).ConfigureAwait(false);
                _logger.LogTrace($"IMAP client connected to {_receiverOptions.ImapHost}.");
                if (_imapClient is ImapClient client)
                    client.Capabilities &= ~_receiverOptions.CapabilitiesToRemove;
                if (_imapClient.Capabilities.HasFlag(ImapCapabilities.Compress))
                    await _imapClient.CompressAsync(cancellationToken).ConfigureAwait(false);
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

        /// <exception cref="FolderNotFoundException">No mail folder has the specified name</exception>
        public async ValueTask<IMailFolder> ConnectMailFolderAsync(CancellationToken cancellationToken = default)
        {
            _ = await ConnectAuthenticatedImapClientAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogTrace($"Connecting to mail folder: '{_receiverOptions.MailFolderName}'.");
            var mailFolder = string.IsNullOrWhiteSpace(_receiverOptions.MailFolderName) || _receiverOptions.MailFolderName.Equals("INBOX", StringComparison.OrdinalIgnoreCase) ?
                _imapClient.Inbox : await _imapClient.GetFolderAsync(_receiverOptions.MailFolderName, cancellationToken).ConfigureAwait(false);
            if (_receiverOptions.MailFolderAccess != FolderAccess.None)
            {
                _ = await mailFolder.OpenAsync(_receiverOptions.MailFolderAccess, cancellationToken).ConfigureAwait(false);
                _logger.LogTrace($"{this} mail folder opened with {_receiverOptions.MailFolderAccess} access.");
            }
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

        /// <summary>
        /// Add flags with checks to make sure the folder is open and writeable.
        /// If there's a delete flag then it calls the Expunge method.
        /// </summary>
        /// <param name="uniqueIds">UniqueIDs to download.</param>
        /// <param name="messageFlags"><see cref="MessageFlags"/> to add.</param>
        /// <param name="silent">Does not emit an <see cref="IMailFolder.MessageFlagsChanged"/> event if set.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public async Task AddFlagsAsync(IEnumerable<UniqueId> uniqueIds, MessageFlags messageFlags, bool silent = true, CancellationToken cancellationToken = default)
        {
            var mailFolder = await ConnectMailFolderAsync(cancellationToken).ConfigureAwait(false);
            bool peekFolder = !mailFolder.IsOpen;
            if (peekFolder || mailFolder.Access != FolderAccess.ReadWrite)
                _ = await mailFolder.OpenAsync(FolderAccess.ReadWrite, cancellationToken).ConfigureAwait(false);
            var ascendingIds = uniqueIds is IList<UniqueId> ids ? ids : uniqueIds.OrderBy(u => u.Id).ToList();
            await mailFolder.AddFlagsAsync(ascendingIds, messageFlags, silent, cancellationToken).ConfigureAwait(false);
            bool delete = messageFlags.HasFlag(MessageFlags.Deleted);
            if (peekFolder)
                await mailFolder.CloseAsync(delete, cancellationToken).ConfigureAwait(false);
            else if (delete)
                await mailFolder.ExpungeAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogDebug($"{messageFlags} flag(s) added to {ascendingIds.Count} message(s) in {_receiverOptions}.");
        }

        public IImapReceiver Clone()
        {
            var receiverOptions = _receiverOptions.Copy();
            return Create(receiverOptions, _logger, _imapLogger);
        }

        public override string ToString() => _receiverOptions.ToString();

        public async Task DisconnectAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogTrace("Disconnecting IMAP email client...");
            if (_imapClient.IsConnected)
                await _imapClient.DisconnectAsync(true, cancellationToken).ConfigureAwait(false);
        }

        public async ValueTask DisposeAsync()
        {
            await DisconnectAsync(CancellationToken.None).ConfigureAwait(false);
            _imapClient.Dispose();
            _loggerFactory.Dispose();
        }

        public void Dispose()
        {
            DisconnectAsync(CancellationToken.None).GetAwaiter().GetResult();
            _imapClient.Dispose();
            _loggerFactory.Dispose();
        }
    }
}
