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

        private readonly SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1);

        private ILogger _logger;
        private IMailFolder _mailFolder = null;
        private IList<string> SentFolderNames;
        private readonly IMailFolderCache _mailFolderCache;
        private readonly IList<string> DraftsFolderNames;
        private readonly IList<string> JunkFolderNames;
        private readonly IList<string> TrashFolderNames;
        private readonly IImapReceiver _imapReceiver;

        public MailFolderClient(IImapReceiver imapReceiver, IOptions<FolderClientOptions> options = null, ILogger<MailFolderClient> logger = null, IMailFolderCache mailFolderCache = null)
        {
            _logger = logger ?? NullLogger<MailFolderClient>.Instance;
            _mailFolderCache = mailFolderCache;
            _imapReceiver = imapReceiver ?? throw new ArgumentNullException(nameof(imapReceiver));
            SentFolderNames = options?.Value?.SentFolderNames ?? FolderClientOptions.CommonSentFolderNames;
            DraftsFolderNames = options?.Value?.DraftsFolderNames ?? FolderClientOptions.CommonDraftsFolderNames;
            JunkFolderNames = options?.Value?.JunkFolderNames ?? FolderClientOptions.CommonJunkFolderNames;
            TrashFolderNames = options?.Value?.TrashFolderNames ?? FolderClientOptions.CommonTrashFolderNames;
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
                _logger.LogTrace($"{this} | {mailFolder} mail folder opened with {folderAccess} access.");
            }
            else if (enableWrite && mailFolder.Access != FolderAccess.ReadWrite)
            {
                _logger.LogTrace($"{this} | {mailFolder} mail folder SyncRoot changed for ReadWrite access.");
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

        public IMailFolder SentFolder => GetFolder(SpecialFolder.Sent).Value;

        public IMailFolder DraftsFolder => GetFolder(SpecialFolder.Drafts).Value;

        public IMailFolder JunkFolder => GetFolder(SpecialFolder.Junk).Value;

        public IMailFolder TrashFolder => GetFolder(SpecialFolder.Trash).Value;

        private Lazy<IMailFolder> GetFolder(SpecialFolder specialFolder) => new Lazy<IMailFolder>(() =>
        {
            IMailFolder folder = null;
            var client = _imapReceiver.ImapClient;
            if ((client.Capabilities & (ImapCapabilities.SpecialUse | ImapCapabilities.XList)) != 0)
            {
                lock (client.SyncRoot)
                    folder = client.GetFolder(specialFolder);
            }
            else
            {
                IList<string> folderNames;
                switch (specialFolder)
                {
                    case SpecialFolder.Sent:
                        folderNames = SentFolderNames;
                        break;
                    case SpecialFolder.Drafts:
                        folderNames = DraftsFolderNames;
                        break;
                    case SpecialFolder.Junk:
                        folderNames = JunkFolderNames;
                        break;
                    case SpecialFolder.Trash:
                        folderNames = TrashFolderNames;
                        break;
                    // All, Archive, Flagged, Important
                    default:
                        throw new NotImplementedException();
                };
                lock (client.SyncRoot)
                    folder = client.GetFolder(client.PersonalNamespaces[0]);
                lock (folder.SyncRoot)
                    folder = folder.GetSubfolders(false, CancellationToken.None).FirstOrDefault(x =>
                        folderNames.Contains(x.Name, StringComparer.OrdinalIgnoreCase));
            }
            return folder;
        });

        public async Task<IMailFolder> GetFolderAsync(string mailFolderFullName, bool createIfNotFound = false, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(mailFolderFullName))
                return null;
            var imapClient = await _imapReceiver.ConnectAuthenticatedImapClientAsync(cancellationToken).ConfigureAwait(false);
            IMailFolder mailFolder = null;
            try
            {
                mailFolder = await imapClient.GetFolderAsync(mailFolderFullName, cancellationToken).ConfigureAwait(false);
            }
            catch (FolderNotFoundException ex)
            {
                if (createIfNotFound)
                {
                    _logger.LogDebug("{ImapReceiver} {MailFolder} folder not found, creating it now.", _imapReceiver, mailFolderFullName);
                    var namespaceFolder = imapClient.PersonalNamespaces.FirstOrDefault()
                        ?? imapClient.SharedNamespaces.FirstOrDefault()
                        ?? imapClient.OtherNamespaces.FirstOrDefault();
                    var baseFolder = namespaceFolder != null ? imapClient.GetFolder(namespaceFolder) : imapClient.Inbox;
                    mailFolder = await baseFolder.CreateAsync(mailFolderFullName, isMessageFolder: true, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    _logger.LogDebug(ex, "{ImapReceiver} {MailFolder} folder.", _imapReceiver, mailFolderFullName);
                }
            }
            return mailFolder;
        }

        /// <inheritdoc />
        public async Task<IMailFolder> GetFolderAsync(IEnumerable<string> folderNames = null, CancellationToken cancellationToken = default)
        {
            if (folderNames == null && _mailFolder != null)
            {
                return _mailFolder;
            }
            IMailFolder mailFolder = null;
            _semaphoreSlim.Wait();
            try
            {
                var imapClient = await _imapReceiver.ConnectAuthenticatedImapClientAsync(cancellationToken).ConfigureAwait(false);
                if (folderNames == null || !folderNames.Any() || folderNames.Contains("INBOX", StringComparer.OrdinalIgnoreCase))
                {
                    mailFolder = imapClient.Inbox;
                }
                else
                {
                    var folderNamespaces = Enumerable.Concat(Enumerable.Concat(imapClient.PersonalNamespaces, imapClient.SharedNamespaces), imapClient.OtherNamespaces);
                    foreach (var folderNamespace in folderNamespaces)
                    {
                        var folders = await imapClient.GetFoldersAsync(folderNamespace, subscribedOnly: true, cancellationToken).ConfigureAwait(false);
                        foreach (var mFolder in folders ?? Enumerable.Empty<IMailFolder>())
                        {
                            var subfolders = await mFolder.GetSubfoldersAsync(subscribedOnly: false, cancellationToken).ConfigureAwait(false);
                            mailFolder = subfolders.FirstOrDefault(x => folderNames.Contains(x.Name, StringComparer.OrdinalIgnoreCase));
                            if (cancellationToken.IsCancellationRequested || mailFolder != null)
                                break;
                        }
                        if (cancellationToken.IsCancellationRequested || mailFolder != null)
                            break;
                    }
                }
            }
            finally
            {
                _semaphoreSlim.Release();
            }
            return mailFolder;
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
                if (source == null)
                    source = await ConnectAsync(enableWrite: move, cancellationToken).ConfigureAwait(false);
                bool peekSourceFolder = !source.IsOpen;
                // Beware, source must be opened after destination to keep it open
                _ = await ConnectMailFolderAsync(source, enableWrite: move, cancellationToken).ConfigureAwait(false);
                resultUid = await source.MoveToAsync(messageUid, destination, cancellationToken).ConfigureAwait(false);
                _logger.LogTrace("{0} {1} {2} to {3} in {4}.", _imapReceiver, messageUid, verb, resultUid, destination.FullName);
                if (peekSourceFolder && source.IsOpen)
                    await source.CloseAsync(expunge: false, cancellationToken).ConfigureAwait(false);
                // destination folder is already closed at this point
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{ImapReceiverFolder} {MessageUid} not {Verb} to {DestinationFolder}.", _imapReceiver, messageUid, verb, destination);
            }
            return resultUid;
        }

        private async Task<UniqueId?> MoveOrCopyAsync(UniqueId messageUid, string destinationFolderFullName, bool move = true, CancellationToken cancellationToken = default)
        {
            var destination = await GetFolderAsync(destinationFolderFullName, move, cancellationToken).ConfigureAwait(false);
            var resultUid = await MoveOrCopyAsync(messageUid, _mailFolder, destination, move, cancellationToken).ConfigureAwait(false);
            return resultUid;
        }

        private async Task<UniqueIdMap> MoveOrCopyAsync(IEnumerable<UniqueId> uniqueIds, IMailFolder source, IMailFolder destination, bool move = true, CancellationToken cancellationToken = default)
        {
            UniqueIdMap resultUids = null;
            string verb = move ? "moved" : "copied";
            try
            {
                if (uniqueIds == null)
                    throw new ArgumentNullException(nameof(uniqueIds));
                if (destination == null)
                    throw new ArgumentNullException(nameof(destination));
                if (source == null)
                    source = await ConnectAsync(enableWrite: move, cancellationToken).ConfigureAwait(false);
                // Beware, source must be opened after destination to keep it open
                bool peekSourceFolder = !source.IsOpen;
                _ = await ConnectMailFolderAsync(source, enableWrite: move, cancellationToken).ConfigureAwait(false);
                var ascendingIds = uniqueIds is IList<UniqueId> ids ? ids : new UniqueIdSet(uniqueIds, SortOrder.Ascending);
                resultUids = await source.MoveToAsync(ascendingIds, destination, cancellationToken).ConfigureAwait(false);
                _logger.LogTrace("{0} {1} {2} to {3} in {4}.", _imapReceiver, ascendingIds, verb, resultUids, destination.FullName);
                if (peekSourceFolder)
                    await source.CloseAsync(expunge: false, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{ImapReceiverFolder} {MessageUid} not {Verb} to {DestinationFolder}.", _imapReceiver, uniqueIds, verb, destination);
            }
            return resultUids ?? UniqueIdMap.Empty;
        }

        private async Task<UniqueIdMap> MoveOrCopyAsync(IEnumerable<UniqueId> messageUids, string destinationFolderFullName, bool move = true, CancellationToken cancellationToken = default)
        {
            var destination = await GetFolderAsync(destinationFolderFullName, move, cancellationToken).ConfigureAwait(false);
            var resultUids = await MoveOrCopyAsync(messageUids, _mailFolder, destination, move, cancellationToken).ConfigureAwait(false);
            return resultUids;
        }

        public async Task<UniqueId?> AppendSentMessageAsync(MimeMessage message, MessageFlags messageFlags = MessageFlags.Seen, CancellationToken cancellationToken = default, ITransferProgress transferProgress = default) =>
            await SentFolder.AppendAsync(message, messageFlags, cancellationToken, transferProgress).ConfigureAwait(false);

        public async Task<UniqueId?> CopyToAsync(IMessageSummary messageSummary, SpecialFolder mailFolder = SpecialFolder.Drafts, CancellationToken cancellationToken = default) =>
            await MoveOrCopyAsync(messageSummary.UniqueId, messageSummary.Folder, GetFolder(mailFolder).Value, move: false, cancellationToken).ConfigureAwait(false);

        public async Task<UniqueId?> CopyToAsync(UniqueId messageUid, IMailFolder destination, CancellationToken cancellationToken = default) =>
            await MoveOrCopyAsync(messageUid, _mailFolder, destination, move: false, cancellationToken).ConfigureAwait(false);

        public async Task<UniqueId?> CopyToAsync(UniqueId messageUid, string destinationFolderFullName, CancellationToken cancellationToken = default) =>
            await MoveOrCopyAsync(messageUid, destinationFolderFullName, move: false, cancellationToken).ConfigureAwait(false);

        public async Task<UniqueIdMap> CopyToAsync(IEnumerable<UniqueId> messageUids, IMailFolder destination, CancellationToken cancellationToken = default) =>
            await MoveOrCopyAsync(messageUids, _mailFolder, destination, move: false, cancellationToken).ConfigureAwait(false);

        public async Task<UniqueIdMap> CopyToAsync(IEnumerable<UniqueId> messageUids, string destinationFolderFullName, CancellationToken cancellationToken = default) =>
            await MoveOrCopyAsync(messageUids, destinationFolderFullName, move: false, cancellationToken).ConfigureAwait(false);

        public async Task<UniqueId?> MoveToAsync(UniqueId messageUid, IMailFolder destination, CancellationToken cancellationToken = default) =>
            await MoveOrCopyAsync(messageUid, _mailFolder, destination, move: true, cancellationToken).ConfigureAwait(false);

        public async Task<UniqueId?> MoveToAsync(UniqueId messageUid, string destinationFolderFullName, CancellationToken cancellationToken = default) =>
            await MoveOrCopyAsync(messageUid, destinationFolderFullName, move: true, cancellationToken).ConfigureAwait(false);

        [Obsolete("Use MoveToAsync with messageSummary.UniqueId instead.")]
        public async Task<UniqueId?> MoveToAsync(IMessageSummary messageSummary, SpecialFolder mailFolder = SpecialFolder.Sent, CancellationToken cancellationToken = default) =>
            await MoveOrCopyAsync(messageSummary.UniqueId, messageSummary.Folder, GetFolder(mailFolder).Value, move: true, cancellationToken).ConfigureAwait(false);

        [Obsolete("Use MoveToAsync with messageSummary.UniqueId instead.")]
        public async Task<UniqueId?> MoveToAsync(IMessageSummary messageSummary, string destinationFolderFullName, CancellationToken cancellationToken = default) =>
            await MoveOrCopyAsync(messageSummary.UniqueId, destinationFolderFullName, move: true, cancellationToken).ConfigureAwait(false);

        public async Task<UniqueIdMap> MoveToAsync(IEnumerable<UniqueId> messageUids, IMailFolder destination, CancellationToken cancellationToken = default) =>
            await MoveOrCopyAsync(messageUids, _mailFolder, destination, move: true, cancellationToken).ConfigureAwait(false);

        public async Task<UniqueIdMap> MoveToAsync(IEnumerable<UniqueId> messageUids, string destinationFolder, CancellationToken cancellationToken = default) =>
            await MoveOrCopyAsync(messageUids, destinationFolder, move: true, cancellationToken).ConfigureAwait(false);

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
