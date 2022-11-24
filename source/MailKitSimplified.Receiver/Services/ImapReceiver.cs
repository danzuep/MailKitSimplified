using System;
using System.Net;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging.Abstractions;
using MailKit;
using MailKit.Security;
using MailKit.Net.Imap;
using MailKitSimplified.Receiver.Abstractions;
using MailKitSimplified.Receiver.Models;
using MailKitSimplified.Receiver.Extensions;

namespace MailKitSimplified.Receiver.Services
{
    public sealed class ImapReceiver : IImapReceiver
    {
        private readonly ILogger _logger;
        private readonly IImapClient _imapClient;
        private readonly EmailReceiverOptions _receiverOptions;

        public ImapReceiver(IOptions<EmailReceiverOptions> receiverOptions, ILogger<ImapReceiver> logger = null, IProtocolLogger protocolLogger = null, IImapClient imapClient = null)
        {
            _logger = logger ?? NullLogger<ImapReceiver>.Instance;
            _receiverOptions = receiverOptions.Value;
            if (string.IsNullOrWhiteSpace(_receiverOptions.ImapHost))
                throw new NullReferenceException($"{nameof(EmailReceiverOptions.ImapHost)} is null.");
            if (_receiverOptions.ImapCredential == null)
                throw new NullReferenceException($"{nameof(EmailReceiverOptions.ImapCredential)} is null.");
            var imapLogger = protocolLogger ?? new MailKitProtocolLogger();
            if (imapLogger is MailKitProtocolLogger imapLog)
                imapLog.SetLogFilePath(_receiverOptions.ProtocolLog);
            _imapClient = imapClient ?? new ImapClient(imapLogger);

        }

        public static ImapReceiver Create(string imapHost, ushort imapPort = 0, string username = null, string password = null, string protocolLog = null, string mailFolderName = null)
        {
            var imapCredential = new NetworkCredential(username, password);
            var receiver = Create(imapHost, imapCredential, imapPort, protocolLog);
            return receiver;
        }

        public static ImapReceiver Create(string imapHost, NetworkCredential imapCredential, ushort imapPort = 0, string protocolLog = null, string mailFolderName = null)
        {
            var receiverOptions = new EmailReceiverOptions(imapHost, imapCredential, imapPort, protocolLog, mailFolderName);
            var receiver = Create(receiverOptions);
            return receiver;
        }

        public static ImapReceiver Create(EmailReceiverOptions emailReceiverOptions, ILogger<ImapReceiver> logger = null)
        {
            var options = Options.Create(emailReceiverOptions);
            var receiver = new ImapReceiver(options, logger);
            return receiver;
        }

        public IMailFolderReader ReadMail => MailFolderReader.Create(this, _receiverOptions.MailFolderName);

        public IMailFolderReader ReadFrom(string mailFolderName)
        {
            var mailFolderReader = MailFolderReader.Create(this, mailFolderName);
            return mailFolderReader;
        }

        /// <exception cref="AuthenticationException">Failed to authenticate</exception>
        public async ValueTask<IImapClient> ConnectImapClientAsync(CancellationToken cancellationToken = default)
        {
            if (!_imapClient.IsConnected)
            {
                await _imapClient.ConnectAsync(_receiverOptions.ImapHost, _receiverOptions.ImapPort, SecureSocketOptions.Auto, cancellationToken).ConfigureAwait(false);
                if (_imapClient.Capabilities.HasFlag(ImapCapabilities.Compress))
                    await _imapClient.CompressAsync(cancellationToken).ConfigureAwait(false);
                _logger.LogTrace($"IMAP client connected to {_receiverOptions.ImapHost}.");
            }
            if (!_imapClient.IsAuthenticated)
            {
                // Pre-emptively disable XOAUTH2 authentication since we don't have an OAuth2 token.
                _imapClient.AuthenticationMechanisms.Remove("XOAUTH2");
                var ntlm = _imapClient.AuthenticationMechanisms.Contains("NTLM") ?
                    new SaslMechanismNtlm(_receiverOptions.ImapCredential) : null;
                if (ntlm?.Workstation != null)
                    await _imapClient.AuthenticateAsync(ntlm, cancellationToken).ConfigureAwait(false);
                else
                    await _imapClient.AuthenticateAsync(_receiverOptions.ImapCredential, cancellationToken).ConfigureAwait(false);
                _logger.LogTrace($"IMAP client authenticated with {_receiverOptions.ImapHost}.");
            }
            return _imapClient;
        }

        /// <exception cref="FolderNotFoundException">No mail folder has the specified name</exception>
        public async ValueTask<IMailFolder> ConnectMailFolderAsync(string mailFolderName = null, CancellationToken cancellationToken = default)
        {
            _ = await ConnectImapClientAsync(cancellationToken).ConfigureAwait(false);
            var targetMailFolder = mailFolderName ?? _receiverOptions.MailFolderName;
            _logger.LogTrace($"Target mail folder: '{targetMailFolder}'.");
            var mailFolder = string.IsNullOrWhiteSpace(targetMailFolder) || targetMailFolder.Equals("INBOX", StringComparison.OrdinalIgnoreCase) ?
                _imapClient.Inbox : await _imapClient.GetFolderAsync(targetMailFolder, cancellationToken).ConfigureAwait(false);
            return mailFolder;
        }

        public async ValueTask<IMailFolderClient> ConnectMailFolderClientAsync(string mailFolderName = null, CancellationToken cancellationToken = default)
        {
            var mailFolder = await ConnectMailFolderAsync(mailFolderName, cancellationToken).ConfigureAwait(false);
            var mailFolderClient = new MailFolderClient(mailFolder);
            return mailFolderClient;
        }

        public async ValueTask<IList<string>> GetMailFolderNamesAsync(CancellationToken cancellationToken = default)
        {
            _ = await ConnectImapClientAsync(cancellationToken).ConfigureAwait(false);
            IList<string> mailFolderNames = new List<string>();
            if (_imapClient.PersonalNamespaces.Count > 0)
            {
                var rootFolder = await _imapClient.GetFoldersAsync(_imapClient.PersonalNamespaces[0], cancellationToken: cancellationToken).ConfigureAwait(false);
                var subfolders = rootFolder.SelectMany(rf => rf.GetSubfolders().Select(sf => sf.Name));
                var inboxSubfolders = _imapClient.Inbox.GetSubfolders().Select(f => f.FullName);
                mailFolderNames.AddRange(inboxSubfolders);
                mailFolderNames.AddRange(subfolders);
                _logger.LogDebug($"{inboxSubfolders.Count()} Inbox folders: {inboxSubfolders.ToEnumeratedString()}.");
                _logger.LogDebug($"{subfolders.Count()} personal folders: {subfolders.ToEnumeratedString()}.");
            }
            if (_imapClient.SharedNamespaces.Count > 0)
            {
                var rootFolder = await _imapClient.GetFoldersAsync(_imapClient.SharedNamespaces[0], cancellationToken: cancellationToken).ConfigureAwait(false);
                var subfolders = rootFolder.SelectMany(rf => rf.GetSubfolders().Select(sf => sf.Name));
                mailFolderNames.AddRange(subfolders);
                _logger.LogDebug($"{subfolders.Count()} shared folders: {subfolders.ToEnumeratedString()}.");
            }
            if (_imapClient.OtherNamespaces.Count > 0)
            {
                var rootFolder = await _imapClient.GetFoldersAsync(_imapClient.OtherNamespaces[0], cancellationToken: cancellationToken).ConfigureAwait(false);
                var subfolders = rootFolder.SelectMany(rf => rf.GetSubfolders().Select(sf => sf.Name));
                mailFolderNames.AddRange(subfolders);
                _logger.LogDebug($"{subfolders.Count()} other folders: {subfolders.ToEnumeratedString()}.");
            }
            return mailFolderNames;
        }

        public override string ToString() => _receiverOptions.ToString();

        public async ValueTask DisconnectAsync(CancellationToken cancellationToken = default)
        {
            if (_imapClient?.IsConnected ?? false)
                await _imapClient.DisconnectAsync(true, cancellationToken).ConfigureAwait(false);
        }

        public async ValueTask DisposeAsync()
        {
            _logger.LogTrace("Disposing IMAP email client...");
            await DisconnectAsync().ConfigureAwait(false);
            _imapClient?.Dispose();
        }

        public void Disconnect(CancellationToken cancellationToken = default)
        {
            if (_imapClient?.IsConnected ?? false)
                lock (_imapClient.SyncRoot)
                    _imapClient.Disconnect(true, cancellationToken);
        }

        public void Dispose()
        {
            _logger.LogTrace("Disposing IMAP email client...");
            Disconnect();
            _imapClient?.Dispose();
        }
    }
}
