using MailKit;
using MailKit.Search;
using MailKit.Net.Imap;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
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
        private readonly ILogger _logger;
        private readonly IImapReceiver _imapReceiver;

        public MailFolderClient(IImapReceiver imapReceiver, ILogger<MailFolderClient> logger = null)
        {
            _logger = logger ?? NullLogger<MailFolderClient>.Instance;
            _imapReceiver = imapReceiver ?? throw new ArgumentNullException(nameof(imapReceiver));
        }

        public static MailFolderClient Create(EmailReceiverOptions emailReceiverOptions, ILogger<MailFolderClient> logger = null, ILogger<ImapReceiver> logImap = null, IProtocolLogger protocolLogger = null, IImapClient imapClient = null)
        {
            var imapReceiver = ImapReceiver.Create(emailReceiverOptions, logImap, protocolLogger, imapClient);
            var mailFolderClient = new MailFolderClient(imapReceiver, logger);
            return mailFolderClient;
        }

        public static MailFolderClient Create(IImapClient imapClient, EmailReceiverOptions emailReceiverOptions, ILogger<MailFolderClient> logger = null, ILogger<ImapReceiver> logImap = null)
        {
            var imapReceiver = ImapReceiver.Create(imapClient, emailReceiverOptions, logImap);
            var mailFolderClient = new MailFolderClient(imapReceiver, logger);
            return mailFolderClient;
        }

        public async ValueTask<IMailFolder> ConnectAsync(bool enableWrite = false, CancellationToken cancellationToken = default)
        {
            if (_mailFolder == null)
                _mailFolder = await _imapReceiver.ConnectMailFolderAsync(cancellationToken).ConfigureAwait(false);
            if (!_mailFolder.IsOpen)
            {
                var folderAccess = enableWrite ? FolderAccess.ReadWrite : FolderAccess.ReadOnly;
                _ = await _mailFolder.OpenAsync(folderAccess, cancellationToken).ConfigureAwait(false);
                _logger.LogTrace($"{this} mail folder opened with {folderAccess} access.");
            }
            else if (enableWrite && _mailFolder.Access != FolderAccess.ReadWrite)
            {
                _logger.LogTrace($"{this} mail folder SyncRoot changed for ReadWrite access.");
                await _mailFolder.OpenAsync(FolderAccess.ReadWrite, cancellationToken).ConfigureAwait(false);
            }
            return _mailFolder;
        }

        public async Task<int> AddFlagsAsync(IEnumerable<UniqueId> uniqueIds, MessageFlags messageFlags, bool silent = true, CancellationToken cancellationToken = default)
        {
            bool peekFolder = !_mailFolder?.IsOpen ?? true;
            _ = await ConnectAsync(true, cancellationToken).ConfigureAwait(false);
            var ascendingIds = uniqueIds is IList<UniqueId> ids ? ids : uniqueIds.OrderBy(u => u.Id).ToList();
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

        public IMailFolder GetSentFolder(CancellationToken cancellationToken = default)
        {
            IMailFolder sentFolder = null;
            if ((_imapReceiver.ImapClient.Capabilities & (ImapCapabilities.SpecialUse | ImapCapabilities.XList)) != 0)
            {
                lock (_imapReceiver.ImapClient.SyncRoot)
                    sentFolder = _imapReceiver.ImapClient.GetFolder(SpecialFolder.Sent);
            }
            else
            {
                string[] commonSentFolderNames = { "Sent Items", "Sent Mail", "Sent Messages" };
                lock (_imapReceiver.ImapClient.SyncRoot)
                    sentFolder = _imapReceiver.ImapClient.GetFolder(_imapReceiver.ImapClient.PersonalNamespaces[0]);
                lock (sentFolder.SyncRoot)
                    sentFolder = sentFolder.GetSubfolders(false, cancellationToken).FirstOrDefault(x =>
                        commonSentFolderNames.Contains(x.Name, StringComparer.OrdinalIgnoreCase));
            }
            return sentFolder;
        }

        public async Task<UniqueId?> MoveToAsync(UniqueId messageUid, IMailFolder destination, CancellationToken cancellationToken = default)
        {
            UniqueId? resultUid = null;
            try
            {
                if (!messageUid.IsValid)
                    throw new ArgumentException("IMessageSummary UniqueId is invalid.");
                if (destination == null)
                    throw new ArgumentNullException(nameof(destination));
                bool peekSourceFolder = !_mailFolder?.IsOpen ?? true;
                _ = await ConnectAsync(true, cancellationToken).ConfigureAwait(false);
                resultUid = await _mailFolder.MoveToAsync(messageUid, destination, cancellationToken).ConfigureAwait(false);
                _logger.LogTrace("{0} {1} moved to {2} in {3}.", _imapReceiver, messageUid, resultUid, destination.FullName);
                if (peekSourceFolder)
                    await _mailFolder.CloseAsync(expunge: false, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{ImapReceiverFolder} {MessageUid} not moved to {DestinationFolder}.", _imapReceiver, messageUid, destination);
            }
            return resultUid;
        }

        public async Task<UniqueId?> MoveToAsync(UniqueId messageUid, string destinationFolder, CancellationToken cancellationToken = default)
        {
            UniqueId? resultUid = null;
            if (messageUid.IsValid && !string.IsNullOrWhiteSpace(destinationFolder))
            {
                try
                {
                    var destination = await _imapReceiver.ImapClient.GetFolderAsync(destinationFolder, cancellationToken).ConfigureAwait(false);
                    resultUid = await _mailFolder.MoveToAsync(messageUid, destination, cancellationToken).ConfigureAwait(false);
                    await destination.CloseAsync(expunge: false, cancellationToken).ConfigureAwait(false);
                }
                catch (FolderNotFoundException ex)
                {
                    _logger.LogWarning(ex, "{DestinationFolder} folder not found, {MessageUid} not moved from {ImapReceiverFolder}.", destinationFolder, messageUid, _imapReceiver);
                }
            }
            return resultUid;
        }

        public async Task<UniqueIdMap> MoveToAsync(IEnumerable<UniqueId> messageUids, IMailFolder destination, CancellationToken cancellationToken = default)
        {
            UniqueIdMap result = null;
            try
            {
                if (messageUids == null)
                    throw new ArgumentNullException(nameof(messageUids));
                if (destination == null)
                    throw new ArgumentNullException(nameof(destination));
                bool peekDestinationFolder = !destination.IsOpen;
                if (peekDestinationFolder || destination.Access != FolderAccess.ReadWrite)
                    _ = await destination.OpenAsync(FolderAccess.ReadWrite, cancellationToken).ConfigureAwait(false);
                bool peekSourceFolder = !_mailFolder?.IsOpen ?? true;
                _ = await ConnectAsync(true, cancellationToken).ConfigureAwait(false);
                var ascendingIds = messageUids is IList<UniqueId> ids ? ids : messageUids.OrderBy(u => u.Id).ToList();
                result = await _mailFolder.MoveToAsync(ascendingIds, destination, cancellationToken).ConfigureAwait(false);
                _logger.LogTrace("{0} moved {1} to {2}.", _imapReceiver, ascendingIds, destination.FullName);
                if (peekSourceFolder)
                    await _mailFolder.CloseAsync(expunge: false, cancellationToken).ConfigureAwait(false);
                if (peekDestinationFolder)
                    await destination.CloseAsync(expunge: false, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{ImapReceiverFolder} {MessageUid} not moved to {DestinationFolder}.", _imapReceiver, messageUids, destination);
            }
            return result ?? UniqueIdMap.Empty;
        }

        public async Task<UniqueIdMap> MoveToAsync(IEnumerable<UniqueId> messageUids, string destinationFolder, CancellationToken cancellationToken = default)
        {
            UniqueIdMap result = null;
            if (messageUids != null && !string.IsNullOrWhiteSpace(destinationFolder))
            {
                try
                {
                    var destination = await _imapReceiver.ImapClient.GetFolderAsync(destinationFolder, cancellationToken).ConfigureAwait(false);
                    result = await MoveToAsync(messageUids, destination, cancellationToken).ConfigureAwait(false);
                }
                catch (FolderNotFoundException ex)
                {
                    _logger.LogError(ex, "{ImapReceiverFolder} {MessageUids} not moved to {DestinationFolder}.", _imapReceiver, messageUids, destinationFolder);
                }
            }
            return result ?? UniqueIdMap.Empty;
        }

        /// <summary>
        /// Query the server for the unique IDs of messages with properties that match the search filters.
        /// </summary>
        /// <param name="searchQuery">Mail folder search query.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The first 250 <see cref="UniqueId"/>s.</returns>
        [Obsolete("Consider using IMailReader.Query() instead.")]
        public async Task<IList<UniqueId>> SearchAsync(SearchQuery searchQuery, CancellationToken cancellationToken = default)
        {
            _ = await ConnectAsync(false, cancellationToken).ConfigureAwait(false);
            var uniqueIds = await _mailFolder.SearchAsync(searchQuery, cancellationToken).ConfigureAwait(false);
            return uniqueIds;
        }

        [Obsolete("Consider using MailFolderReader.Query() instead.")]
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
