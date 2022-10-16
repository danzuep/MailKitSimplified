using MailKit;
using MailKit.Security;
using MailKit.Net.Imap;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MailKitSimplified.Receiver.Abstractions;
using MailKitSimplified.Receiver.Models;
using System.Collections.Generic;
using System.Linq;
using MailKitSimplified.Receiver.Extensions;

namespace MailKitSimplified.Receiver.Services
{
    public class ImapClientService : IImapClientService
    {
        private readonly ILogger _logger;
        private readonly IImapClient _imapClient;
        private readonly EmailReceiverOptions _emailReceiverOptions;

        public ImapClientService(IOptions<EmailReceiverOptions> receiverOptions, ILogger<ImapClientService> logger = null)
        {
            _logger = logger ?? NullLogger<ImapClientService>.Instance;
            _emailReceiverOptions = receiverOptions.Value;
            if (string.IsNullOrWhiteSpace(_emailReceiverOptions.ImapHost))
                throw new NullReferenceException(nameof(EmailReceiverOptions.ImapHost));
            if (_emailReceiverOptions.ImapCredential == null)
                throw new NullReferenceException(nameof(EmailReceiverOptions.ImapCredential));
            if (!string.IsNullOrWhiteSpace(_emailReceiverOptions.ProtocolLog))
                Directory.CreateDirectory(Path.GetDirectoryName(_emailReceiverOptions.ProtocolLog));
            var imapLogger = GetProtocolLogger(_emailReceiverOptions.ProtocolLog);
            _imapClient = imapLogger != null ? new ImapClient(imapLogger) : new ImapClient();
        }

        public static ImapClientService Create(string imapHost, ushort imapPort = 0, string username = null, string password = null, string protocolLog = null)
        {
            var imapCredential = username == null && password == null ? null : new NetworkCredential(username ?? "", password ?? "");
            var receiver = Create(imapHost, imapCredential, imapPort, protocolLog);
            return receiver;
        }

        public static ImapClientService Create(string imapHost, NetworkCredential imapCredential, ushort imapPort = 0, string protocolLog = null)
        {
            var receiverOptions = new EmailReceiverOptions(imapHost, imapCredential, imapPort, protocolLog);
            var receiver = Create(receiverOptions);
            return receiver;
        }

        public static ImapClientService Create(EmailReceiverOptions receiverOptions)
        {
            var options = Options.Create(receiverOptions);
            var receiver = new ImapClientService(options);
            return receiver;
        }

        private static IProtocolLogger GetProtocolLogger(string logFilePath = null)
        {
            var protocolLogger = logFilePath == null ? null :
                string.IsNullOrWhiteSpace(logFilePath) ?
                    new ProtocolLogger(Console.OpenStandardError()) :
                        new ProtocolLogger(logFilePath);
            return protocolLogger;
        }

        /// <exception cref="AuthenticationException">Failed to authenticate</exception>
        public virtual async ValueTask AuthenticateAsync(CancellationToken cancellationToken = default)
        {
            if (!_imapClient.IsConnected)
            {
                await _imapClient.ConnectAsync(_emailReceiverOptions.ImapHost, _emailReceiverOptions.ImapPort, SecureSocketOptions.Auto, cancellationToken).ConfigureAwait(false);
                if (_imapClient.Capabilities.HasFlag(ImapCapabilities.Compress))
                    await _imapClient.CompressAsync(cancellationToken).ConfigureAwait(false);
            }
            if (!_imapClient.IsAuthenticated)
            {
                var ntlm = _imapClient.AuthenticationMechanisms.Contains("NTLM") ?
                    new SaslMechanismNtlm(_emailReceiverOptions.ImapCredential) : null;
                if (ntlm?.Workstation != null)
                    await _imapClient.AuthenticateAsync(ntlm, cancellationToken).ConfigureAwait(false);
                else
                    await _imapClient.AuthenticateAsync(_emailReceiverOptions.ImapCredential, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <exception cref="AuthenticationException">Failed to authenticate</exception>
        /// <exception cref="FolderNotFoundException">No mail folder has the specified name</exception>
        public async ValueTask<IMailFolder> ConnectAsync(CancellationToken ct = default)
        {
            await AuthenticateAsync(ct).ConfigureAwait(false);
            var folderName = _emailReceiverOptions.MailFolderName;
            var mailFolder = string.IsNullOrWhiteSpace(folderName) || folderName.Equals("INBOX", StringComparison.OrdinalIgnoreCase) ?
                _imapClient.Inbox : await _imapClient.GetFolderAsync(folderName, ct).ConfigureAwait(false);
            return mailFolder;
        }

        protected async Task<IList<string>> GetFolderListAsync(CancellationToken ct = default)
        {
            IList<string> mailFolderNames = new List<string>();
            await AuthenticateAsync(ct).ConfigureAwait(false);
            if (_imapClient.PersonalNamespaces.Count > 0)
            {
                var rootFolder = await _imapClient.GetFoldersAsync(_imapClient.PersonalNamespaces[0]);
                var subfolders = rootFolder.SelectMany(rf => rf.GetSubfolders().Select(sf => sf.Name));
                var inboxSubfolders = _imapClient.Inbox.GetSubfolders().Select(f => f.FullName);
                mailFolderNames.AddRange(inboxSubfolders);
                mailFolderNames.AddRange(subfolders);
                _logger.LogDebug("{0} Inbox folders: {1}", subfolders.Count(), inboxSubfolders.ToEnumeratedString());
                _logger.LogDebug("{0} personal folders: {1}", subfolders.Count(), subfolders.ToEnumeratedString());
            }
            if (_imapClient.SharedNamespaces.Count > 0)
            {
                var rootFolder = await _imapClient.GetFoldersAsync(_imapClient.SharedNamespaces[0]);
                var subfolders = rootFolder.SelectMany(rf => rf.GetSubfolders().Select(sf => sf.Name));
                mailFolderNames.AddRange(subfolders);
                _logger.LogDebug("{0} shared folders: {1}", subfolders.Count(), subfolders.ToEnumeratedString());
            }
            if (_imapClient.OtherNamespaces.Count > 0)
            {
                var rootFolder = await _imapClient.GetFoldersAsync(_imapClient.OtherNamespaces[0]);
                var subfolders = rootFolder.SelectMany(rf => rf.GetSubfolders().Select(sf => sf.Name));
                mailFolderNames.AddRange(subfolders);
                _logger.LogDebug("{0} other folders: {1}", subfolders.Count(), subfolders.ToEnumeratedString());
            }
            return mailFolderNames;
        }

        public virtual void Disconnect()
        {
            if (_imapClient?.IsConnected ?? false)
            {
                lock (_imapClient.SyncRoot)
                    _imapClient?.Disconnect(true);
            }
        }

        public virtual void Dispose()
        {
            _logger.LogTrace("Disposing of the IMAP email receiver client...");
            Disconnect();
            _imapClient?.Dispose();
        }
    }
}
