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
        private SearchQuery _searchQuery = _queryAll;
        private static readonly SearchQuery _queryAll = SearchQuery.All;
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

        public async Task<IList<IMessageSummary>> GetMessageSummariesAsync(MessageSummaryItems filter, CancellationToken cancellationToken = default)
        {
            if (_take == 0)
                return Array.Empty<IMessageSummary>();

            filter |= MessageSummaryItems.UniqueId;
            var mailFolder = await _mailFolderClient.ConnectAsync(false, cancellationToken).ConfigureAwait(false);
            _ = await mailFolder.OpenAsync(FolderAccess.ReadOnly, cancellationToken).ConfigureAwait(false);
            IList<IMessageSummary> filteredSummaries;
            int skip = _skip < mailFolder.Count ? _skip : mailFolder.Count;
            if (_searchQuery != _queryAll)
            {
                var uniqueIds = await mailFolder.SearchAsync(_searchQuery, cancellationToken).ConfigureAwait(false);
                var descendingUids = uniqueIds.OrderByDescending(u => u.Id).Skip(skip);
                var filteredUids = _take == _all ? descendingUids : descendingUids.Take(_take);
                var ascendingUids = filteredUids.Reverse().ToList();
                var messageSummaries = await mailFolder.FetchAsync(ascendingUids, filter, cancellationToken).ConfigureAwait(false);
                filteredSummaries = messageSummaries.Where(m => uniqueIds.Contains(m.UniqueId)).ToList();
            }
            else
            {
                int endIndex = _take < 0 ? _all : skip + _take - 1;
                filteredSummaries = await mailFolder.FetchAsync(skip, endIndex, filter, cancellationToken).ConfigureAwait(false);
            }
            if (_continueTake && _take > 0)
            {
                _skip += _take;
            }
            await _mailFolderClient.DisposeAsync().ConfigureAwait(false);

            return filteredSummaries;
        }

        public async Task<IList<IMessageSummary>> GetMessageSummariesAsync(CancellationToken cancellationToken = default) =>
            await GetMessageSummariesAsync(CoreMessageItems, cancellationToken).ConfigureAwait(false);

        public async Task<IList<MimeMessage>> GetMimeMessagesAsync(CancellationToken cancellationToken = default, ITransferProgress transferProgress = null)
        {
            if (_take == 0)
                return Array.Empty<MimeMessage>();

            var mimeMessages = new List<MimeMessage>();
            var mailFolder = await _mailFolderClient.ConnectAsync(false, cancellationToken).ConfigureAwait(false);
            _ = await mailFolder.OpenAsync(FolderAccess.ReadOnly, cancellationToken).ConfigureAwait(false);
            int skip = _skip < mailFolder.Count ? _skip : mailFolder.Count;
            if (_take == _all || _searchQuery != _queryAll)
            {
                var uids = await mailFolder.SearchAsync(_searchQuery, cancellationToken).ConfigureAwait(false);
                var descendingUids = uids.OrderByDescending(u => u.Id).Skip(skip);
                foreach (var uid in descendingUids)
                {
                    if (cancellationToken.IsCancellationRequested) break;
                    var mimeMessage = await mailFolder.GetMessageAsync(uid, cancellationToken, transferProgress).ConfigureAwait(false);
                    mimeMessages.Add(mimeMessage);
                }
            }
            else
            {
                int endIndex = skip + _take > mailFolder.Count ? mailFolder.Count : skip + _take;
                for (int index = skip; index < endIndex; index++)
                {
                    if (cancellationToken.IsCancellationRequested) break;
                    var mimeMessage = await mailFolder.GetMessageAsync(index, cancellationToken, transferProgress).ConfigureAwait(false);
                    mimeMessages.Add(mimeMessage);
                }
            }
            await _mailFolderClient.DisposeAsync().ConfigureAwait(false);

            return mimeMessages;
        }

        public async Task<IList<IMessageSummary>> GetMessageSummariesAsync(IEnumerable<UniqueId> uniqueIds, MessageSummaryItems filter = MessageSummaryItems.UniqueId, CancellationToken cancellationToken = default)
        {
            if (_take == 0 || uniqueIds == null)
                return Array.Empty<IMessageSummary>();

            IList<IMessageSummary> filteredSummaries;
            filter |= MessageSummaryItems.UniqueId;
            var mailFolder = await _mailFolderClient.ConnectAsync(false, cancellationToken).ConfigureAwait(false);
            var ascendingUids = uniqueIds.OrderBy(u => u.Id).ToList();
            var messageSummaries = await mailFolder.FetchAsync(ascendingUids, filter, cancellationToken).ConfigureAwait(false);
            filteredSummaries = messageSummaries.Where(m => uniqueIds.Contains(m.UniqueId)).ToList();
            await _mailFolderClient.DisposeAsync().ConfigureAwait(false);

            return filteredSummaries ?? Array.Empty<IMessageSummary>();
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
