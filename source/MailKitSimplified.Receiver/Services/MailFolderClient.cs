using MimeKit;
using MailKit;
using MailKit.Search;
using MailKit.Net.Imap;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MailKitSimplified.Receiver.Abstractions;
using MailKitSimplified.Receiver.Extensions;
using MailKitSimplified.Receiver.Models;

namespace MailKitSimplified.Receiver.Services
{
    public sealed class MailFolderClient : IMailFolderClient
    {
        public string MailFolderName => _mailFolder?.FullName ?? _imapReceiver.ToString();
        public int MailFolderCount => _mailFolder?.Count ?? 0;

        private IMailFolder _mailFolder = null;
        private IList<string> SentFolderNames;
        private IList<string> DraftsFolderNames;
        private ILogger _logger;
        private readonly IImapReceiver _imapReceiver;

        public MailFolderClient(IImapReceiver imapReceiver, IOptions<FolderClientOptions> options = null, ILogger<MailFolderClient> logger = null)
        {
            _logger = logger ?? NullLogger<MailFolderClient>.Instance;
            _imapReceiver = imapReceiver ?? throw new ArgumentNullException(nameof(imapReceiver));
            SentFolderNames = options?.Value?.SentFolderNames ?? FolderClientOptions.CommonSentFolderNames;
            DraftsFolderNames = options?.Value?.DraftsFolderNames ?? FolderClientOptions.CommonDraftsFolderNames;
        }

        public static MailFolderClient Create(EmailReceiverOptions emailReceiverOptions, ILogger<MailFolderClient> logger = null, ILogger<ImapReceiver> logImap = null, IProtocolLogger protocolLogger = null, IImapClient imapClient = null)
        {
            var imapReceiver = ImapReceiver.Create(emailReceiverOptions, logImap, protocolLogger, imapClient);
            var mailFolderClient = new MailFolderClient(imapReceiver, options: null, logger);
            return mailFolderClient;
        }

        public static MailFolderClient Create(IImapClient imapClient, EmailReceiverOptions emailReceiverOptions, ILogger<MailFolderClient> logger = null, ILogger<ImapReceiver> logImap = null)
        {
            var imapReceiver = ImapReceiver.Create(imapClient, emailReceiverOptions, logImap);
            var mailFolderClient = new MailFolderClient(imapReceiver, options: null, logger);
            return mailFolderClient;
        }

        /// <summary>
        /// Logging setup for those not using dependency injection.
        /// </summary>
        public MailFolderClient SetLogger(ILogger logger)
        {
            if (logger != null)
                _logger = logger;
            return this;
        }

        /// <summary>
        /// Logging setup for those not using dependency injection.
        /// </summary>
        public MailFolderClient SetLogger(ILoggerFactory loggerFactory)
        {
            if (loggerFactory != null)
                _logger = loggerFactory.CreateLogger<MailFolderClient>();
            return this;
        }

        /// <summary>
        /// Logging setup for those not using dependency injection.
        /// </summary>
        public MailFolderClient SetLogger(Action<ILoggingBuilder> configure = null)
        {
            ILoggerFactory loggerFactory = null;
            if (configure != null)
                loggerFactory = LoggerFactory.Create(configure);
#if DEBUG
            else
                loggerFactory = LoggerFactory.Create(_ => _.SetMinimumLevel(LogLevel.Debug).AddDebug().AddConsole());
#endif
            return SetLogger(loggerFactory);
        }

        /// <summary>
        /// Overwrite the common sent folder names.
        /// </summary>
        public MailFolderClient SetSentFolderNames(IEnumerable<string> sentFolderNames)
        {
            SentFolderNames = sentFolderNames?.ToList() ?? FolderClientOptions.CommonSentFolderNames;
            return this;
        }

        private async ValueTask<IMailFolder> ConnectMailFolderAsync(IMailFolder mailFolder, bool enableWrite = false, CancellationToken cancellationToken = default)
        {
            if (mailFolder == null)
                mailFolder = await _imapReceiver.ConnectMailFolderAsync(cancellationToken).ConfigureAwait(false);
            if (!mailFolder.IsOpen)
            {
                var folderAccess = enableWrite ? FolderAccess.ReadWrite : FolderAccess.ReadOnly;
                _ = await mailFolder.OpenAsync(folderAccess, cancellationToken).ConfigureAwait(false);
                _logger.LogTrace($"{this} mail folder opened with {folderAccess} access.");
            }
            else if (enableWrite && mailFolder.Access != FolderAccess.ReadWrite)
            {
                _logger.LogTrace($"{this} mail folder SyncRoot changed for ReadWrite access.");
                await mailFolder.OpenAsync(FolderAccess.ReadWrite, cancellationToken).ConfigureAwait(false);
            }
            return mailFolder;
        }

        public async ValueTask<IMailFolder> ConnectAsync(bool enableWrite = false, CancellationToken cancellationToken = default)
        {
            _mailFolder = await ConnectMailFolderAsync(null, enableWrite, cancellationToken).ConfigureAwait(false);
            return _mailFolder;
        }

        public async ValueTask<IMailFolder> ConnectAsync(IMailFolder mailFolder, bool enableWrite = false, CancellationToken cancellationToken = default)
        {
            if (_mailFolder != null && _mailFolder.IsOpen)
                await _mailFolder.CloseAsync(expunge: false, cancellationToken).ConfigureAwait(false);
            _mailFolder = await ConnectMailFolderAsync(mailFolder, enableWrite, cancellationToken).ConfigureAwait(false);
            return _mailFolder;
        }

        public async Task<int> AddFlagsAsync(IEnumerable<UniqueId> uniqueIds, MessageFlags messageFlags, bool silent = true, CancellationToken cancellationToken = default)
        {
            bool peekFolder = !_mailFolder?.IsOpen ?? true;
            _ = await ConnectAsync(true, cancellationToken).ConfigureAwait(false);
            var ascendingIds = uniqueIds is IList<UniqueId> ids ? ids : new UniqueIdSet(uniqueIds, SortOrder.Ascending);
            await _mailFolder.AddFlagsAsync(ascendingIds, messageFlags, silent, cancellationToken).ConfigureAwait(false);
            bool delete = messageFlags.HasFlag(MessageFlags.Deleted);
            if (peekFolder)
                await _mailFolder.CloseAsync(delete, cancellationToken).ConfigureAwait(false);
            else if (delete)
                await _mailFolder.ExpungeAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogDebug($"{messageFlags} flag(s) added to {ascendingIds.Count} message(s) in {_imapReceiver}.");
            return ascendingIds.Count;
        }

        public async Task<int> AddFlagsAsync(SearchQuery searchQuery, MessageFlags messageFlags, bool silent = true, CancellationToken cancellationToken = default)
        {
            int count = 0;
            bool peekFolder = !_mailFolder?.IsOpen ?? true;
            _ = await ConnectAsync(true, cancellationToken).ConfigureAwait(false);
            var matchedUids = await _mailFolder.SearchAsync(searchQuery, cancellationToken).ConfigureAwait(false);
            bool delete = messageFlags.HasFlag(MessageFlags.Deleted);
            while (matchedUids.Count > 0 && !cancellationToken.IsCancellationRequested)
            {
                await _mailFolder.AddFlagsAsync(matchedUids, messageFlags, silent, cancellationToken).ConfigureAwait(false);
                if (delete)
                    await _mailFolder.ExpungeAsync(cancellationToken).ConfigureAwait(false);
                count += matchedUids.Count;
                matchedUids = await _mailFolder.SearchAsync(searchQuery, cancellationToken).ConfigureAwait(false);
            }
            if (peekFolder)
                await _mailFolder.CloseAsync(expunge: false, cancellationToken).ConfigureAwait(false);
            return count;
        }

        public async Task<int> DeleteMessagesAsync(IEnumerable<IMessageSummary> messageSummaries, CancellationToken cancellationToken = default) =>
            await AddFlagsAsync(messageSummaries.Select(m => m.UniqueId), MessageFlags.Deleted, silent: true, cancellationToken).ConfigureAwait(false);

        public async Task<int> DeleteMessagesAsync(TimeSpan relativeOffset, SearchQuery filter = null, CancellationToken cancellationToken = default)
        {
            var absolute = relativeOffset.Days > 0 &&
                Math.Abs(relativeOffset.TotalDays % 1) <= (double.Epsilon * 100) ?
                DateTime.Now.Date.Add(-relativeOffset.Duration()) :
                DateTime.Now.Add(-relativeOffset.Duration());
            var searchQuery = filter == null ?
                (SearchQuery)SearchQuery.DeliveredBefore(absolute) :
                SearchQuery.DeliveredBefore(absolute).And(filter);
            return await AddFlagsAsync(searchQuery, MessageFlags.Deleted, silent: true, cancellationToken).ConfigureAwait(false);
        }

        public Lazy<IMailFolder> DraftsFolder => new Lazy<IMailFolder>(() =>
        {
            IMailFolder draftFolder = null;
            var client = _imapReceiver.ImapClient;
            if ((client.Capabilities & (ImapCapabilities.SpecialUse | ImapCapabilities.XList)) != 0)
            {
                lock (client.SyncRoot)
                    draftFolder = client.GetFolder(SpecialFolder.Drafts);
            }
            else
            {
                lock (client.SyncRoot)
                    draftFolder = client.GetFolder(client.PersonalNamespaces[0]);
                lock (draftFolder.SyncRoot)
                    draftFolder = draftFolder.GetSubfolders(false, CancellationToken.None).FirstOrDefault(x =>
                        DraftsFolderNames.Contains(x.Name, StringComparer.OrdinalIgnoreCase));
            }
            return draftFolder;
        });

        public Lazy<IMailFolder> SentFolder => new Lazy<IMailFolder>(() =>
        {
            IMailFolder sentFolder = null;
            var client = _imapReceiver.ImapClient;
            if ((client.Capabilities & (ImapCapabilities.SpecialUse | ImapCapabilities.XList)) != 0)
            {
                lock (client.SyncRoot)
                    sentFolder = client.GetFolder(SpecialFolder.Sent);
            }
            else
            {
                lock (client.SyncRoot)
                    sentFolder = client.GetFolder(client.PersonalNamespaces[0]);
                lock (sentFolder.SyncRoot)
                    sentFolder = sentFolder.GetSubfolders(false, CancellationToken.None).FirstOrDefault(x =>
                        SentFolderNames.Contains(x.Name, StringComparer.OrdinalIgnoreCase));
            }
            return sentFolder;
        });

        public async Task<IMailFolder> GetSentFolderAsync(CancellationToken cancellationToken = default)
        {
            IMailFolder sentFolder = null;
            var client = _imapReceiver.ImapClient;
            if ((client.Capabilities & (ImapCapabilities.SpecialUse | ImapCapabilities.XList)) != 0)
            {
                lock (client.SyncRoot)
                    sentFolder = client.GetFolder(SpecialFolder.Sent);
            }
            else
            {
                var namespaceFolders = await client.GetFoldersAsync(client.PersonalNamespaces[0]).ConfigureAwait(false);
                var namespaceSubfolders = await namespaceFolders[0].GetSubfoldersAsync(false, cancellationToken).ConfigureAwait(false);
                sentFolder = namespaceSubfolders.FirstOrDefault(x => SentFolderNames.Contains(x.Name, StringComparer.OrdinalIgnoreCase));
            }
            return sentFolder;
        }

        public async Task<UniqueId?> AppendSentMessageAsync(MimeMessage message, MessageFlags messageFlags = MessageFlags.Seen, CancellationToken cancellationToken = default, ITransferProgress transferProgress = default) =>
            await SentFolder.Value.AppendAsync(message, messageFlags, cancellationToken, transferProgress).ConfigureAwait(false);

        internal async Task<UniqueId?> MoveOrCopyAsync(UniqueId messageUid, IMailFolder source, IMailFolder destination, bool move = true, CancellationToken cancellationToken = default)
        {
            UniqueId? resultUid = null;
            string verb = move ? "moved" : "copied";
            try
            {
                if (!messageUid.IsValid)
                    throw new ArgumentException("IMessageSummary UniqueId is invalid.");
                if (destination == null)
                    throw new ArgumentNullException(nameof(destination));
                bool peekSourceFolder = !source?.IsOpen ?? true;
                bool peekDestinationFolder = !destination.IsOpen;
                _ = await ConnectMailFolderAsync(source, enableWrite: false, cancellationToken).ConfigureAwait(false);
                _ = await ConnectMailFolderAsync(destination, enableWrite: true, cancellationToken).ConfigureAwait(false);
                resultUid = await source.MoveToAsync(messageUid, destination, cancellationToken).ConfigureAwait(false);
                _logger.LogTrace("{0} {1} {2} to {3} in {4}.", _imapReceiver, messageUid, verb, resultUid, destination.FullName);
                if (peekSourceFolder)
                    await source.CloseAsync(expunge: false, cancellationToken).ConfigureAwait(false);
                if (peekDestinationFolder)
                    await destination.CloseAsync(expunge: false, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{ImapReceiverFolder} {MessageUid} not {Verb} to {DestinationFolder}.", _imapReceiver, messageUid, verb, destination);
            }
            return resultUid;
        }

        private async Task<UniqueId?> MoveOrCopyAsync(UniqueId messageUid, string destinationFolder, bool move = true, CancellationToken cancellationToken = default)
        {
            UniqueId? resultUid = null;
            if (messageUid.IsValid && !string.IsNullOrWhiteSpace(destinationFolder))
            {
                try
                {
                    var destination = await _imapReceiver.ImapClient.GetFolderAsync(destinationFolder, cancellationToken).ConfigureAwait(false);
                    resultUid = await MoveOrCopyAsync(messageUid, _mailFolder, destination, move, cancellationToken).ConfigureAwait(false);
                    await destination.CloseAsync(expunge: false, cancellationToken).ConfigureAwait(false);
                }
                catch (FolderNotFoundException ex)
                {
                    string verb = move ? "moved" : "copied";
                    _logger.LogWarning(ex, "{DestinationFolder} folder not found, {MessageUid} not {Verb} from {ImapReceiverFolder}.", destinationFolder, messageUid, verb, _imapReceiver);
                }
            }
            return resultUid;
        }

        private async Task<UniqueIdMap> MoveOrCopyAsync(IEnumerable<UniqueId> uniqueIds, IMailFolder destination, bool move = true, CancellationToken cancellationToken = default)
        {
            UniqueIdMap result = null;
            string verb = move ? "moved" : "copied";
            try
            {
                if (uniqueIds == null)
                    throw new ArgumentNullException(nameof(uniqueIds));
                if (destination == null)
                    throw new ArgumentNullException(nameof(destination));
                bool peekDestinationFolder = !destination.IsOpen;
                if (peekDestinationFolder || destination.Access != FolderAccess.ReadWrite)
                    _ = await destination.OpenAsync(FolderAccess.ReadWrite, cancellationToken).ConfigureAwait(false);
                bool peekSourceFolder = !_mailFolder?.IsOpen ?? true;
                _ = await ConnectAsync(true, cancellationToken).ConfigureAwait(false);
                var ascendingIds = uniqueIds is IList<UniqueId> ids ? ids : new UniqueIdSet(uniqueIds, SortOrder.Ascending);
                result = await _mailFolder.MoveToAsync(ascendingIds, destination, cancellationToken).ConfigureAwait(false);
                _logger.LogTrace("{0} {1} {2} to {3}.", _imapReceiver, ascendingIds, verb, destination.FullName);
                if (peekSourceFolder)
                    await _mailFolder.CloseAsync(expunge: false, cancellationToken).ConfigureAwait(false);
                if (peekDestinationFolder)
                    await destination.CloseAsync(expunge: false, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{ImapReceiverFolder} {MessageUid} not {Verb} to {DestinationFolder}.", _imapReceiver, uniqueIds, verb, destination);
            }
            return result ?? UniqueIdMap.Empty;
        }

        private async Task<UniqueIdMap> MoveOrCopyAsync(IEnumerable<UniqueId> messageUids, string destinationFolder, bool move = true, CancellationToken cancellationToken = default)
        {
            UniqueIdMap result = null;
            if (messageUids != null && !string.IsNullOrWhiteSpace(destinationFolder))
            {
                try
                {
                    var destination = await _imapReceiver.ImapClient.GetFolderAsync(destinationFolder, cancellationToken).ConfigureAwait(false);
                    result = await MoveOrCopyAsync(messageUids, destination, move, cancellationToken).ConfigureAwait(false);
                }
                catch (FolderNotFoundException ex)
                {
                    string verb = move ? "moved" : "copied";
                    _logger.LogError(ex, "{ImapReceiverFolder} {MessageUid} not {Verb} to {DestinationFolder}.", _imapReceiver, messageUids, verb, destinationFolder);
                }
            }
            return result ?? UniqueIdMap.Empty;
        }

        public async Task<UniqueId?> CopyToAsync(UniqueId messageUid, IMailFolder destination, CancellationToken cancellationToken = default) =>
            await MoveOrCopyAsync(messageUid, _mailFolder, destination, move: false, cancellationToken).ConfigureAwait(false);

        public async Task<UniqueId?> CopyToAsync(UniqueId messageUid, string destinationFolder, CancellationToken cancellationToken = default) =>
            await MoveOrCopyAsync(messageUid, destinationFolder, move: false, cancellationToken).ConfigureAwait(false);

        public async Task<UniqueIdMap> CopyToAsync(IEnumerable<UniqueId> messageUids, IMailFolder destination, CancellationToken cancellationToken = default) =>
            await MoveOrCopyAsync(messageUids, destination, move: false, cancellationToken).ConfigureAwait(false);

        public async Task<UniqueIdMap> CopyToAsync(IEnumerable<UniqueId> messageUids, string destinationFolder, CancellationToken cancellationToken = default) =>
            await MoveOrCopyAsync(messageUids, destinationFolder, move: false, cancellationToken).ConfigureAwait(false);

        public async Task<UniqueId?> MoveToAsync(UniqueId messageUid, IMailFolder destination, CancellationToken cancellationToken = default) =>
            await MoveOrCopyAsync(messageUid, _mailFolder, destination, move: true, cancellationToken).ConfigureAwait(false);

        public async Task<UniqueId?> MoveToAsync(UniqueId messageUid, string destinationFolder, CancellationToken cancellationToken = default) =>
            await MoveOrCopyAsync(messageUid, destinationFolder, move: true, cancellationToken).ConfigureAwait(false);

        public async Task<UniqueIdMap> MoveToAsync(IEnumerable<UniqueId> messageUids, IMailFolder destination, CancellationToken cancellationToken = default) =>
            await MoveOrCopyAsync(messageUids, destination, move: true, cancellationToken).ConfigureAwait(false);

        public async Task<UniqueIdMap> MoveToAsync(IEnumerable<UniqueId> messageUids, string destinationFolder, CancellationToken cancellationToken = default) =>
            await MoveOrCopyAsync(messageUids, destinationFolder, move: true, cancellationToken).ConfigureAwait(false);

        /// <summary>
        /// Query the server for the unique IDs of messages with properties that match the search filters.
        /// </summary>
        /// <param name="searchQuery">Mail folder search query.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The first 250 <see cref="UniqueId"/>s.</returns>
        [Obsolete("Use IMailReader.Query().GetMessageSummariesAsync() instead.")]
        public async Task<IList<UniqueId>> SearchAsync(SearchQuery searchQuery, CancellationToken cancellationToken = default)
        {
            _ = await ConnectAsync(false, cancellationToken).ConfigureAwait(false);
            var uniqueIds = await _mailFolder.SearchAsync(searchQuery, cancellationToken).ConfigureAwait(false);
            return uniqueIds;
        }

        [Obsolete("Use MailFolderReader.Top(1).GetMessageSummariesAsync() with FirstOrDefault() instead.")]
        public async Task<IMessageSummary> GetNewestMessageSummaryAsync(MessageSummaryItems filter = MessageSummaryItems.UniqueId, CancellationToken cancellationToken = default)
        {
            _ = await ConnectAsync(true, cancellationToken).ConfigureAwait(false);
            var index = _mailFolder.Count > 0 ? _mailFolder.Count - 1 : _mailFolder.Count;
            var messageSummaries = await _mailFolder.FetchAsync(index, index, filter, cancellationToken).ConfigureAwait(false);
            await _mailFolder.CloseAsync(false, CancellationToken.None).ConfigureAwait(false);
            return messageSummaries.FirstOrDefault();
        }

        public IMailFolderClient Copy() => MemberwiseClone() as IMailFolderClient;

        public override string ToString() => $"{MailFolderName} ({MailFolderCount})";

        public async ValueTask DisposeAsync()
        {
            if (_mailFolder?.IsOpen ?? false)
                await _mailFolder.CloseAsync(false, CancellationToken.None).ConfigureAwait(false);
        }

        public void Dispose()
        {
            if (_mailFolder?.IsOpen ?? false)
                lock (_mailFolder.SyncRoot)
                    _mailFolder.Close(false, CancellationToken.None);
        }
    }
}
