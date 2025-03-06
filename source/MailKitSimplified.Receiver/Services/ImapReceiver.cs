﻿using MailKit;
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
    /// <inheritdoc cref="IImapReceiver" />
    public sealed class ImapReceiver : IImapReceiver
    {
        private Lazy<MailFolderClient> _mailFolderClient;
        private Lazy<MailFolderReader> _mailFolderReader;
        private Lazy<IMailFolderMonitor> _mailFolderMonitor;
        private bool _isClientInjected;
        private IImapClient _imapClient;
        private IProtocolLogger _imapLogger;
        private ILogger<ImapReceiver> _logger;
        private ILoggerFactory _loggerFactory;
        private EmailReceiverOptions _receiverOptions;

        /// <inheritdoc cref="IImapReceiver" />
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
            _mailFolderClient = new Lazy<MailFolderClient>(() => Services.MailFolderClient.Create(
                _imapClient, _receiverOptions, _loggerFactory.CreateLogger<MailFolderClient>(), _loggerFactory.CreateLogger<ImapReceiver>()));
            _mailFolderReader = new Lazy<MailFolderReader>(() => MailFolderReader.Create(
                _imapClient, _receiverOptions, _loggerFactory.CreateLogger<MailFolderReader>(), _loggerFactory.CreateLogger<ImapReceiver>()));
            _mailFolderMonitor = new Lazy<IMailFolderMonitor>(() => MailFolderMonitor.Create(
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
                _isClientInjected = true;
            }
            else if (_imapLogger != null)
                _imapClient = new ImapClient(_imapLogger);
            else
                _imapClient = new ImapClient();
            _mailFolderClient = new Lazy<MailFolderClient>(() => Services.MailFolderClient.Create(
                _imapClient, _receiverOptions, _loggerFactory.CreateLogger<MailFolderClient>(), _loggerFactory.CreateLogger<ImapReceiver>()));
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
            _mailFolderMonitor = new Lazy<IMailFolderMonitor>(() => MailFolderMonitor.Create(
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
        /// Logging setup for those not using dependency injection.
        /// </summary>
        public ImapReceiver SetLogger(ILoggerFactory loggerFactory, ILogger<ImapReceiver> logger = null)
        {
            _loggerFactory = loggerFactory ?? _loggerFactory ?? NullLoggerFactory.Instance;
            _logger = logger ?? _logger ?? _loggerFactory.CreateLogger<ImapReceiver>();
            return SetOptions(_receiverOptions);
        }

        /// <summary>
        /// Logging setup for those not using dependency injection.
        /// </summary>
        public ImapReceiver SetLogger(Action<ILoggingBuilder> configure = null)
        {
            ILoggerFactory loggerFactory = null;
            if (configure != null)
                loggerFactory = LoggerFactory.Create(configure);
#if DEBUG
            else
                loggerFactory = LoggerFactory.Create(_ => _.SetMinimumLevel(LogLevel.Debug).AddDebug().AddConsole());
#endif
            return loggerFactory != null ? SetLogger(loggerFactory, null) : this;
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
            _receiverOptions.CustomAuthenticationMethod = customAuthenticationMethod;
            return this;
        }

        public ImapReceiver SetCustomAuthentication(SaslMechanism saslMechanism)
        {
            _receiverOptions.AuthenticationMechanism = saslMechanism;
            return this;
        }

        public IMailFolderClient GetFolder(string mailFolderName) //TODO add IMailFolderClient overload
        {
            _receiverOptions.MailFolderName = mailFolderName;
            return MailFolderClient;
        }

        public IMailFolderReader ReadFrom(string mailFolderName) //TODO add IMailFolderReader overload
        {
            _receiverOptions.MailFolderName = mailFolderName;
            return ReadMail;
        }

        public IMailFolderMonitor Monitor(string mailFolderName, IMailFolderMonitor mailFolderMonitor = null)
        {
            _receiverOptions.MailFolderName = mailFolderName;
            if (mailFolderMonitor != null)
                _mailFolderMonitor = new Lazy<IMailFolderMonitor>(() => mailFolderMonitor);
            return MonitorFolder;
        }

        public IMailFolderClient MailFolderClient => _mailFolderClient.Value;

        public IMailFolderReader ReadMail => _mailFolderReader.Value;

        public IMailFolderMonitor MonitorFolder => _mailFolderMonitor.Value;

        public IImapClient ImapClient => _imapClient;

        public async ValueTask<IImapClient> ConnectAuthenticatedImapClientAsync(CancellationToken cancellationToken = default, bool force = false)
        {
            await ConnectImapClientAsync(force: false, cancellationToken).ConfigureAwait(false);
            await AuthenticateImapClientAsync(force: false, cancellationToken).ConfigureAwait(false);
            return _imapClient;
        }

        internal async ValueTask ConnectImapClientAsync(bool force, CancellationToken cancellationToken = default)
        {
            if (force || !_imapClient.IsConnected)
            {
                await _imapClient.ConnectAsync(_receiverOptions.ImapHost, _receiverOptions.ImapPort, _receiverOptions.SocketOptions, cancellationToken).ConfigureAwait(false);
                _logger.LogTrace($"IMAP client connected to {_receiverOptions.ImapHost}.");
                if (_receiverOptions.CapabilitiesToRemove != ImapCapabilities.None)
                    _imapClient.Capabilities &= ~_receiverOptions.CapabilitiesToRemove;
                if (_imapClient.Capabilities.HasFlag(ImapCapabilities.Compress))
                    await _imapClient.CompressAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Authenticating via a SASL mechanism may be a multi-step process.
        /// <see href="http://www.mimekit.net/docs/html/T_MailKit_Security_SaslMechanism.htm"/>
        /// <seealso href="http://www.mimekit.net/docs/html/T_MailKit_Security_SaslMechanismOAuth2.htm"/>
        /// <seealso href="https://github.com/jstedfast/MailKit/blob/master/ExchangeOAuth2.md"/>
        /// <seealso href="https://github.com/jstedfast/MailKit/blob/master/GMailOAuth2.md"/>
        /// </summary>
        internal async ValueTask AuthenticateImapClientAsync(bool force, CancellationToken cancellationToken = default)
        {
            if (force || !_imapClient.IsAuthenticated)
            {
                if (_receiverOptions.CustomAuthenticationMethod != null) // for XOAUTH2 and OAUTHBEARER
                    await _receiverOptions.CustomAuthenticationMethod(_imapClient).ConfigureAwait(false);
                else if (_receiverOptions.AuthenticationMechanism != null)
                    await _imapClient.AuthenticateAsync(_receiverOptions.AuthenticationMechanism).ConfigureAwait(false);
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

        /// <inheritdoc cref="ImapClient.GetFolderAsync"/>
        public async ValueTask<IMailFolder> ConnectMailFolderAsync(CancellationToken cancellationToken = default)
        {
            _ = await ConnectAuthenticatedImapClientAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogTrace($"Connecting to mail folder: '{_receiverOptions.MailFolderName}'.");
            IMailFolder mailFolder;
            if (string.IsNullOrWhiteSpace(_receiverOptions.MailFolderName))
            {
                var namespaceFolder = _imapClient.PersonalNamespaces.FirstOrDefault()
                    ?? _imapClient.SharedNamespaces.FirstOrDefault()
                    ?? _imapClient.OtherNamespaces.FirstOrDefault();
                if (string.IsNullOrWhiteSpace(namespaceFolder?.Path))
                {
                    mailFolder = _imapClient.Inbox;
                }
                else
                {
                    mailFolder = _imapClient.GetFolder(namespaceFolder);
                    _receiverOptions.MailFolderName = mailFolder.FullName;
                }
            }
            else if (_receiverOptions.MailFolderName.Equals("INBOX", StringComparison.OrdinalIgnoreCase))
                mailFolder = _imapClient.Inbox;
            else
                mailFolder = await _imapClient.GetFolderAsync(_receiverOptions.MailFolderName, cancellationToken).ConfigureAwait(false);
            if (_receiverOptions.MailFolderAccess != FolderAccess.None)
            {
                _ = await mailFolder.OpenAsync(_receiverOptions.MailFolderAccess, cancellationToken).ConfigureAwait(false);
                _logger.LogTrace($"{this} mail folder opened with {_receiverOptions.MailFolderAccess} access.");
                //_receiverOptions.MailFolderAccess = FolderAccess.None;
            }
            return mailFolder;
        }

        private async Task<IList<string>> GetAllSubfoldersAsync(FolderNamespaceCollection folderNamespaceCollection, string folderGroup, CancellationToken cancellationToken = default)
        {
            var mailFolderNames = new List<string>();
            foreach (var folderNamespace in folderNamespaceCollection)
            {
                var subfolders = await _imapClient.GetAllSubfoldersAsync(folderNamespace, cancellationToken).ConfigureAwait(false);
                var subfolderNames = subfolders.Select(sf => $"\"{sf.FullName}\"");
                mailFolderNames.AddRange(subfolderNames);
                _logger.LogDebug($"{subfolders.Count} {folderGroup} folders: {subfolderNames.ToEnumeratedString()}.");
            }
            return mailFolderNames;
        }

        public async Task<IList<string>> GetMailFolderNamesAsync(CancellationToken cancellationToken = default)
        {
            _ = await ConnectAuthenticatedImapClientAsync(cancellationToken).ConfigureAwait(false);
            var mailFolderNames = new List<string>();
            var inboxSubfolders = await _imapClient.Inbox.GetSubfoldersAsync(subscribedOnly: false, cancellationToken).ConfigureAwait(false);
            if (inboxSubfolders?.Count > 0)
            {
                var inboxSubfolderNames = inboxSubfolders.Select(sf => $"\"{sf.FullName}\"");
                mailFolderNames.AddRange(inboxSubfolderNames);
                _logger.LogDebug($"{inboxSubfolders.Count} Inbox folders: {inboxSubfolderNames.ToEnumeratedString()}.");
            }
            if (_imapClient.PersonalNamespaces.Count > 0)
            {
                var subfolderNames = await GetAllSubfoldersAsync(_imapClient.PersonalNamespaces, "personal", cancellationToken).ConfigureAwait(false);
                mailFolderNames.AddRange(subfolderNames);
            }
            if (_imapClient.SharedNamespaces.Count > 0)
            {
                var subfolderNames = await GetAllSubfoldersAsync(_imapClient.SharedNamespaces, "shared", cancellationToken).ConfigureAwait(false);
                mailFolderNames.AddRange(subfolderNames);
            }
            if (_imapClient.OtherNamespaces.Count > 0)
            {
                var subfolderNames = await GetAllSubfoldersAsync(_imapClient.OtherNamespaces, "other", cancellationToken).ConfigureAwait(false);
                mailFolderNames.AddRange(subfolderNames);
            }
            return mailFolderNames;
        }

        [Obsolete("Use MailFolderClient.MoveToAsync method instead")]
        public async Task<UniqueId?> MoveToSentAsync(IMessageSummary messageSummary, CancellationToken cancellationToken = default)
        {
            using (var mailFolderClient = _mailFolderClient.Value)
                return await mailFolderClient.MoveOrCopyAsync(messageSummary.UniqueId, messageSummary.Folder, mailFolderClient.SentFolder, move: true, cancellationToken).ConfigureAwait(false);
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
            if (_mailFolderClient.IsValueCreated)
                _mailFolderClient.Value.Dispose();
            if (_imapClient.IsConnected)
                await _imapClient.DisconnectAsync(true, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Disposes of the ImapClient but not the LoggerFacory, because:
        /// "Services resolved from the container should never be disposed by the developer."
        /// <see href="https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection-guidelines#disposal-of-services"/>
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            await DisconnectAsync(CancellationToken.None).ConfigureAwait(false);
            if (!_isClientInjected)
                _imapClient.Dispose();
        }

        /// <summary>
        /// Disposes of the ImapClient but not the LoggerFacory, because:
        /// "Services resolved from the container should never be disposed by the developer."
        /// <see href="https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection-guidelines#disposal-of-services"/>
        /// </summary>
        public void Dispose()
        {
            DisconnectAsync(CancellationToken.None).GetAwaiter().GetResult();
            if (!_isClientInjected)
                _imapClient.Dispose();
        }
    }
}
