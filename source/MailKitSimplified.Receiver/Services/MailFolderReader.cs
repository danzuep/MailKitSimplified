using MimeKit;
using MailKit;
using MailKit.Search;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using MailKitSimplified.Receiver.Abstractions;

namespace MailKitSimplified.Receiver.Services
{
    public sealed class MailFolderReader : IMailFolderReader
    {
        public static readonly MessageSummaryItems CoreMessageItems =
            MessageSummaryItems.Envelope |
            MessageSummaryItems.BodyStructure |
            MessageSummaryItems.UniqueId;

        private int _skip = 0;
        private int _take = _all;
        private bool _continueTake = false;
        private static readonly int _all = -1;
        private SearchQuery _searchQuery = SearchQuery.All;
        private readonly IMailFolderClient _mailFolderClient;

        public MailFolderReader(IMailFolderClient mailFolderClient)
        {
            _mailFolderClient = mailFolderClient ?? throw new ArgumentNullException(nameof(mailFolderClient));
        }

        public IMailReader Skip(int skipCount)
        {
            if (skipCount < 0)
                throw new ArgumentOutOfRangeException(nameof(skipCount));
            _skip = skipCount;
            return this;
        }

        public IMailReader Take(int takeCount, bool continuous = false)
        {
            if (takeCount < -1)
                throw new ArgumentOutOfRangeException(nameof(takeCount));
            _take = takeCount;
            _continueTake = continuous;
            return this;
        }

        public IMailReader Query(SearchQuery searchQuery)
        {
            _searchQuery = searchQuery;
            return this;
        }

        private async Task<IList<IMessageSummary>> GetMessageSummariesAsync(IMailFolder mailFolder, MessageSummaryItems filter = MessageSummaryItems.UniqueId, CancellationToken cancellationToken = default)
        {
            if (_take == 0)
                return Array.Empty<IMessageSummary>();
            int startIndex = _skip < mailFolder.Count ? _skip : mailFolder.Count;
            int endIndex = _take < 1 ? _all : startIndex + _take - 1;
            var messageSummaries = await mailFolder.FetchAsync(startIndex, endIndex, filter, cancellationToken).ConfigureAwait(false);
            if (_continueTake)
                _skip = endIndex + 1;
            return messageSummaries;
        }

        public async Task<IList<IMessageSummary>> GetMessageSummariesAsync(MessageSummaryItems filter, CancellationToken cancellationToken = default)
        {
            filter |= MessageSummaryItems.UniqueId;
            var mailFolder = await _mailFolderClient.ConnectAsync(false, cancellationToken).ConfigureAwait(false);
            _ = await mailFolder.OpenAsync(FolderAccess.ReadOnly, cancellationToken).ConfigureAwait(false);
            var messageSummaries = await GetMessageSummariesAsync(mailFolder, filter, cancellationToken).ConfigureAwait(false);
            await _mailFolderClient.DisposeAsync().ConfigureAwait(false);
            return messageSummaries;
        }

        public async Task<IList<IMessageSummary>> GetMessageSummariesAsync(CancellationToken cancellationToken = default) =>
            await GetMessageSummariesAsync(CoreMessageItems, cancellationToken).ConfigureAwait(false);

        public async Task<IList<MimeMessage>> GetMimeMessagesAsync(CancellationToken cancellationToken = default, ITransferProgress transferProgress = null)
        {
            var mimeMessages = new List<MimeMessage>();
            var mailFolder = await _mailFolderClient.ConnectAsync(false, cancellationToken).ConfigureAwait(false);
            _ = await mailFolder.OpenAsync(FolderAccess.ReadOnly, cancellationToken).ConfigureAwait(false);
            if (_take == _all)
            {
                var uids = await mailFolder.SearchAsync(_searchQuery, cancellationToken).ConfigureAwait(false);
                foreach (var uid in uids.OrderByDescending(u => u.Id))
                {
                    if (cancellationToken.IsCancellationRequested) break;
                    var mimeMessage = await mailFolder.GetMessageAsync(uid, cancellationToken, transferProgress).ConfigureAwait(false);
                    mimeMessages.Add(mimeMessage);
                }
            }
            else
            {
                int startIndex = _skip < mailFolder.Count ? _skip : mailFolder.Count;
                int endIndex = startIndex + _take > mailFolder.Count ? mailFolder.Count : startIndex + _take;
                for (int index = startIndex; index < endIndex; index++)
                {
                    if (cancellationToken.IsCancellationRequested) break;
                    var mimeMessage = await mailFolder.GetMessageAsync(index, cancellationToken, transferProgress).ConfigureAwait(false);
                    mimeMessages.Add(mimeMessage);
                }
            }
            await _mailFolderClient.DisposeAsync().ConfigureAwait(false);
            return mimeMessages;
        }

        public async Task<IEnumerable<IMessageSummary>> GetMessageSummariesAsync(IEnumerable<UniqueId> uniqueIds, MessageSummaryItems filter = MessageSummaryItems.UniqueId, CancellationToken cancellationToken = default)
        {
            IEnumerable<IMessageSummary> filteredResults = Array.Empty<IMessageSummary>();
            if (uniqueIds != null)
            {
                filter |= MessageSummaryItems.UniqueId;
                var orderedIds = uniqueIds.OrderBy(m => m.Id).ToList();
                var mailFolder = await _mailFolderClient.ConnectAsync(false, cancellationToken).ConfigureAwait(false);
                var messageSummaries = await mailFolder.FetchAsync(orderedIds, filter, cancellationToken).ConfigureAwait(false);
                filteredResults = messageSummaries.Where(m => uniqueIds.Contains(m.UniqueId));
                await _mailFolderClient.DisposeAsync().ConfigureAwait(false);
            }
            return filteredResults;
        }

        public async Task<MimeMessage> GetMimeMessageAsync(UniqueId uniqueId, CancellationToken cancellationToken = default)
        {
            MimeMessage mimeMessage;
            var mailFolder = await _mailFolderClient.ConnectAsync(false, cancellationToken).ConfigureAwait(false);
            mimeMessage = await mailFolder.GetMessageAsync(uniqueId, cancellationToken).ConfigureAwait(false);
            await _mailFolderClient.DisposeAsync().ConfigureAwait(false);
            return mimeMessage;
        }

        public async Task<IList<MimeMessage>> GetMimeMessagesAsync(IEnumerable<UniqueId> uniqueIds, CancellationToken cancellationToken = default)
        {
            IList<MimeMessage> mimeMessages = new List<MimeMessage>();
            if (uniqueIds != null)
            {
                var mailFolder = await _mailFolderClient.ConnectAsync(false, cancellationToken).ConfigureAwait(false);
                foreach (var uniqueId in uniqueIds)
                {
                    if (cancellationToken.IsCancellationRequested) break;
                    var mimeMessage = await mailFolder.GetMessageAsync(uniqueId, cancellationToken).ConfigureAwait(false);
                    if (mimeMessage != null) mimeMessages.Add(mimeMessage);
                }
                await _mailFolderClient.DisposeAsync().ConfigureAwait(false);
            }
            return mimeMessages;
        }

        public IMailFolderReader Copy() => MemberwiseClone() as IMailFolderReader;

        public override string ToString() => $"{_mailFolderClient} (skip {_skip}, take {_take})";
    }
}
