using MimeKit;
using MailKit;
using MailKit.Search;
using MailKit.Net.Imap;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MailKitSimplified.Receiver.Abstractions;
using MailKitSimplified.Receiver.Extensions;
using MailKitSimplified.Receiver.Models;

namespace MailKitSimplified.Receiver.Services
{
    public sealed class MailFolderReader : IMailFolderReader
    {
        /// <summary>
        /// Core message summary items: UniqueId, Envelope, Headers, Size, and BodyStructure.
        /// </summary>
        public static readonly MessageSummaryItems CoreMessageItems =
            MessageSummaryItems.UniqueId |
            MessageSummaryItems.Envelope |
            MessageSummaryItems.Headers |
            MessageSummaryItems.Size |
            MessageSummaryItems.BodyStructure;

        /// <summary>Query just the arrival dates of messages on the server.</summary>
        /// <param name="deliveredAfter">Search for messages after this date.</param>
        /// <param name="deliveredBefore">Search for messages before this date.</param>
        /// <returns><see cref="SearchQuery"/> with a maximum of 250 results.</returns>
        public static SearchQuery QueryBetweenDates(DateTime deliveredAfter, DateTime? deliveredBefore = null)
        {
            DateTime before = deliveredBefore != null ? deliveredBefore.Value : DateTime.Now;
            var query = SearchQuery.DeliveredAfter(deliveredAfter).And(SearchQuery.DeliveredBefore(before));
            return query;
        }

        /// <summary>Query the server for messages with matching keywords in the subject or body text.</summary>
        /// <param name="keywords">Keywords to search for.</param>
        /// <returns><see cref="SearchQuery"/> with a maximum of 250 results.</returns>
        public static SearchQuery QueryKeywords(IEnumerable<string> keywords)
        {
            var subjectMatch = keywords.MatchAny(SearchQuery.SubjectContains);
            var bodyMatch = keywords.MatchAny(SearchQuery.BodyContains);
            var query = subjectMatch.Or(bodyMatch);
            return query;
        }

        /// <summary>Query the server for message(s) with a matching message ID.</summary>
        /// <param name="messageId">Message-ID to search for.</param>
        /// <returns><see cref="SearchQuery"/> with a maximum of 250 results.</returns>
        public static SearchQuery QueryMessageId(string messageId, bool addAngleBrackets = true)
        {
            var searchText = addAngleBrackets ? $"<{messageId}>" : messageId;
            var query = SearchQuery.HeaderContains("Message-Id", searchText);
            return query;
        }

        private int _skip = 0;
        private int _take = _all;
        private bool _continueTake = false;
        private static readonly int _all = -1;
        private static readonly int _queryAmount = 250;
        private SearchQuery _searchQuery = _queryAll;
        private static readonly SearchQuery _queryAll = SearchQuery.All;
        private MessageSummaryItems _messageSummaryItems = MessageSummaryItems.Envelope;
        private readonly ILogger _logger;
        private readonly IImapReceiver _imapReceiver;

        public MailFolderReader(IImapReceiver imapReceiver, ILogger<MailFolderReader> logger = null)
        {
            _logger = logger ?? NullLogger<MailFolderReader>.Instance;
            _imapReceiver = imapReceiver ?? throw new ArgumentNullException(nameof(imapReceiver));
        }

        public static MailFolderReader Create(EmailReceiverOptions emailReceiverOptions, ILogger<MailFolderReader> logger = null, ILogger<ImapReceiver> logImap = null, IProtocolLogger protocolLogger = null, IImapClient imapClient = null)
        {
            if (emailReceiverOptions == null)
                throw new ArgumentNullException(nameof(emailReceiverOptions));
            var imapReceiver = ImapReceiver.Create(emailReceiverOptions, logImap, protocolLogger, imapClient);
            var mailFolderReader = new MailFolderReader(imapReceiver, logger);
            return mailFolderReader;
        }

        public static MailFolderReader Create(IImapClient imapClient, EmailReceiverOptions emailReceiverOptions, ILogger<MailFolderReader> logger = null, ILogger<ImapReceiver> logImap = null)
        {
            var imapReceiver = ImapReceiver.Create(imapClient, emailReceiverOptions, logImap);
            var mailFolderReader = new MailFolderReader(imapReceiver, logger);
            return mailFolderReader;
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

        public IMailReader Items(MessageSummaryItems messageSummaryItems)
        {
            _messageSummaryItems = messageSummaryItems;
            return this;
        }

        private async Task<(IMailFolder, bool)> OpenMailFolderAsync(CancellationToken cancellationToken = default)
        {
            if (_take == 0)
                return (null, false);
            var mailFolder = await _imapReceiver.ConnectMailFolderAsync(cancellationToken).ConfigureAwait(false);
            bool closeWhenFinished = !mailFolder.IsOpen;
            if (!mailFolder.IsOpen)
                _ = await mailFolder.OpenAsync(FolderAccess.ReadWrite, cancellationToken).ConfigureAwait(false);
            if (_skip >= mailFolder.Count || (_skip > _queryAmount && _searchQuery != _queryAll))
            {
                if (_skip < mailFolder.Count)
                {
                    _logger.LogWarning($"Skip({_skip}) limited to mail folder count of {mailFolder.Count}.");
                    if (_continueTake)
                        _skip = mailFolder.Count;
                }
                else
                    _logger.LogWarning($"Skip({_skip}) exceeded SearchQuery limit of 250 results.");
            }
            return (mailFolder, closeWhenFinished);
        }

        private async Task<IList<IMessageSummary>> GetMessageSummariesAsync(IMailFolder mailFolder, MessageSummaryItems filter, CancellationToken cancellationToken = default)
        {
            if (mailFolder == null)
                return Array.Empty<IMessageSummary>();

            IList<IMessageSummary> filteredSummaries;
            filter |= MessageSummaryItems.UniqueId;
            if (_searchQuery != _queryAll)
            {
                if (_take > _queryAmount)
                    _logger.LogWarning($"Take({_take}) limited by SearchQuery to 250 results.");
                var uniqueIds = await mailFolder.SearchAsync(_searchQuery, cancellationToken).ConfigureAwait(false);
                var descendingUids = uniqueIds.OrderByDescending(u => u.Id).Skip(_skip);
                var filteredUids = _take == _all ? descendingUids : descendingUids.Take(_take);
                var ascendingUids = filteredUids.Reverse().ToList();
                var messageSummaries = await mailFolder.FetchAsync(ascendingUids, filter, cancellationToken).ConfigureAwait(false);
                filteredSummaries = messageSummaries.Where(m => uniqueIds.Contains(m.UniqueId)).Reverse().ToList();
            }
            else
            {
                int endIndex = _take < 0 ? _all : _skip + _take - 1;
                filteredSummaries = await mailFolder.FetchAsync(_skip, endIndex, filter, cancellationToken).ConfigureAwait(false);
            }
            _logger.LogTrace($"{_imapReceiver} received {filteredSummaries.Count} email(s).");
            if (_continueTake && _take > 0)
                _skip += _take;

            return filteredSummaries;
        }

        public async Task<IList<IMessageSummary>> GetMessageSummariesAsync(MessageSummaryItems filter, CancellationToken cancellationToken = default)
        {
            if (_take == 0)
                return Array.Empty<IMessageSummary>();
            (var mailFolder, var closeWhenFinished) = await OpenMailFolderAsync(cancellationToken).ConfigureAwait(false);
            var messageSummaries = await GetMessageSummariesAsync(mailFolder, filter, cancellationToken).ConfigureAwait(false);
            if (closeWhenFinished)
                await mailFolder.CloseAsync(false, CancellationToken.None).ConfigureAwait(false);
            return messageSummaries;
        }

        public async Task<IList<IMessageSummary>> GetMessageSummariesAsync(CancellationToken cancellationToken = default) =>
            await GetMessageSummariesAsync(_messageSummaryItems, cancellationToken).ConfigureAwait(false);

        public async Task<IList<MimeMessage>> GetMimeMessagesAsync(CancellationToken cancellationToken = default, ITransferProgress transferProgress = null)
        {
            var mimeMessages = new List<MimeMessage>();
            if (_take == 0)
                return mimeMessages;
            (var mailFolder, var closeWhenFinished) = await OpenMailFolderAsync(cancellationToken).ConfigureAwait(false);
            if (_take == _all || _searchQuery != _queryAll)
            {
                if (_take > _queryAmount)
                    _logger.LogWarning($"Take({_take}) limited by SearchQuery to 250 results.");
                var uniqueIds = await mailFolder.SearchAsync(_searchQuery, cancellationToken).ConfigureAwait(false);
                var descendingUids = uniqueIds.OrderByDescending(u => u.Id).Skip(_skip);
                var filteredUids = _take == _all ? descendingUids : descendingUids.Take(_take);
                foreach (var uid in filteredUids)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;
                    var mimeMessage = await mailFolder.GetMessageAsync(uid, cancellationToken, transferProgress).ConfigureAwait(false);
                    mimeMessages.Add(mimeMessage);
                }
            }
            else
            {
                int endIndex = _skip + _take > mailFolder.Count ? mailFolder.Count : _skip + _take;
                for (int index = _skip; index < endIndex; index++)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;
                    var mimeMessage = await mailFolder.GetMessageAsync(index, cancellationToken, transferProgress).ConfigureAwait(false);
                    mimeMessages.Add(mimeMessage);
                }
            }
            _logger.LogTrace($"{_imapReceiver} received {mimeMessages.Count} email(s).");
            if (_continueTake && _take > 0)
            {
                if (_skip < mailFolder.Count)
                    _skip += _take;
                else
                    _skip = mailFolder.Count;
            }
            if (closeWhenFinished)
                await mailFolder.CloseAsync(false, CancellationToken.None).ConfigureAwait(false);

            return mimeMessages;
        }

        public async Task<IList<IMessageSummary>> GetMessageSummariesAsync(IEnumerable<UniqueId> uniqueIds, MessageSummaryItems filter = MessageSummaryItems.UniqueId, CancellationToken cancellationToken = default)
        {
            if (_take == 0 || uniqueIds == null)
                return Array.Empty<IMessageSummary>();

            IList<IMessageSummary> filteredSummaries;
            filter |= MessageSummaryItems.UniqueId;
            var mailFolder = await _imapReceiver.ConnectMailFolderAsync(cancellationToken).ConfigureAwait(false);
            _ = await mailFolder.OpenAsync(FolderAccess.ReadOnly, cancellationToken).ConfigureAwait(false);
            var ascendingUids = uniqueIds.OrderBy(u => u.Id).ToList();
            var messageSummaries = await mailFolder.FetchAsync(ascendingUids, filter, cancellationToken).ConfigureAwait(false);
            filteredSummaries = messageSummaries.Where(m => uniqueIds.Contains(m.UniqueId)).Reverse().ToList();
            _logger.LogTrace($"{_imapReceiver} received {filteredSummaries.Count} email(s).");
            await mailFolder.CloseAsync(false, CancellationToken.None).ConfigureAwait(false);

            return filteredSummaries ?? Array.Empty<IMessageSummary>();
        }

        public async Task<MimeMessage> GetMimeMessageAsync(UniqueId uniqueId, CancellationToken cancellationToken = default, ITransferProgress progress = null)
        {
            MimeMessage mimeMessage;
            var mailFolder = await _imapReceiver.ConnectMailFolderAsync(cancellationToken).ConfigureAwait(false);
            _ = await mailFolder.OpenAsync(FolderAccess.ReadOnly, cancellationToken).ConfigureAwait(false);
            mimeMessage = await mailFolder.GetMessageAsync(uniqueId, cancellationToken, progress).ConfigureAwait(false);
            _logger.LogTrace($"{_imapReceiver} received {mimeMessage.MessageId}.");
            await mailFolder.CloseAsync(false, CancellationToken.None).ConfigureAwait(false);
            return mimeMessage;
        }

        public async Task<IList<MimeMessage>> GetMimeMessagesAsync(IEnumerable<UniqueId> uniqueIds, CancellationToken cancellationToken = default, ITransferProgress progress = null)
        {
            IList<MimeMessage> mimeMessages = new List<MimeMessage>();
            if (uniqueIds != null)
            {
                var mailFolder = await _imapReceiver.ConnectMailFolderAsync(cancellationToken).ConfigureAwait(false);
                _ = await mailFolder.OpenAsync(FolderAccess.ReadOnly, cancellationToken).ConfigureAwait(false);
                foreach (var uniqueId in uniqueIds)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;
                    var mimeMessage = await mailFolder.GetMessageAsync(uniqueId, cancellationToken, progress).ConfigureAwait(false);
                    if (mimeMessage != null)
                        mimeMessages.Add(mimeMessage);
                }
                _logger.LogTrace($"{_imapReceiver} received {mimeMessages.Count} email(s).");
                await mailFolder.CloseAsync(false, CancellationToken.None).ConfigureAwait(false);
            }
            return mimeMessages;
        }
#if NET5_0_OR_GREATER
        public async IAsyncEnumerable<MimeMessage> GetMimeMessages(IEnumerable<UniqueId> uniqueIds, [EnumeratorCancellation] CancellationToken cancellationToken = default, ITransferProgress progress = null)
        {
            if (uniqueIds != null)
            {
                var mailFolder = await _imapReceiver.ConnectMailFolderAsync(cancellationToken).ConfigureAwait(false);
                _ = mailFolder.Open(FolderAccess.ReadOnly, cancellationToken);
                foreach (var uniqueId in uniqueIds)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;
                    var mimeMessage = await mailFolder.GetMessageAsync(uniqueId, cancellationToken, progress).ConfigureAwait(false);
                    _logger.LogTrace($"{_imapReceiver} received {mimeMessage.MessageId}.");
                    if (mimeMessage != null)
                        yield return mimeMessage;
                }
                mailFolder.Close(false, CancellationToken.None);
            }
        }

        public async Task SaveAllAsync(IEnumerable<UniqueId> uniqueIds, string folderPath, bool createDirectory = false, CancellationToken cancellationToken = default, ITransferProgress progress = null)
        {
            if (createDirectory)
                Directory.CreateDirectory(folderPath);
            else if (!Directory.Exists(folderPath))
                throw new DirectoryNotFoundException($"Directory not found: {folderPath}.");
            var format = FormatOptions.Default.Clone();
            format.NewLineFormat = NewLineFormat.Dos;
            await foreach (var mimeMessage in GetMimeMessages(uniqueIds, cancellationToken, progress))
            {
                string fileName = Path.Combine(folderPath, $"{mimeMessage.MessageId}.eml");
                await mimeMessage.WriteToAsync(format, fileName, cancellationToken).ConfigureAwait(false);
            }
        }
#else
        public async Task SaveAllAsync(IEnumerable<UniqueId> uniqueIds, string folderPath, bool createDirectory = false, CancellationToken cancellationToken = default, ITransferProgress progress = null)
        {
            if (uniqueIds != null)
            {
                if (createDirectory)
                    Directory.CreateDirectory(folderPath);
                else if (!Directory.Exists(folderPath))
                    throw new DirectoryNotFoundException($"Directory not found: {folderPath}.");
                var format = FormatOptions.Default.Clone();
                format.NewLineFormat = NewLineFormat.Dos;
                var mailFolder = await _imapReceiver.ConnectMailFolderAsync(cancellationToken).ConfigureAwait(false);
                _ = mailFolder.Open(FolderAccess.ReadOnly, cancellationToken);
                foreach (var uniqueId in uniqueIds)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;
                    var mimeMessage = await mailFolder.GetMessageAsync(uniqueId, cancellationToken, progress).ConfigureAwait(false);
                    _logger.LogTrace($"{_imapReceiver} received {mimeMessage.MessageId}.");
                    if (mimeMessage != null)
                    {
                        string fileName = Path.Combine(folderPath, $"{mimeMessage.MessageId}.eml");
                        await mimeMessage.WriteToAsync(format, fileName, cancellationToken).ConfigureAwait(false);
                    }
                }
                mailFolder.Close(false, CancellationToken.None);
            }
        }
#endif
        /// <summary>Query just the arrival dates of messages on the server.</summary>
        /// <param name="deliveredAfter">Search for messages after this date.</param>
        /// <param name="deliveredBefore">Search for messages before this date.</param>
        /// <returns>List of <see cref="IMessageSummary"/> items.</returns>
        public async Task<IList<IMessageSummary>> SearchBetweenDatesAsync(DateTime deliveredAfter, DateTime? deliveredBefore = null, CancellationToken cancellationToken = default)
        {
            DateTime before = deliveredBefore != null ? deliveredBefore.Value : DateTime.Now;
            _searchQuery = SearchQuery.DeliveredAfter(deliveredAfter).And(SearchQuery.DeliveredBefore(before));
            var messageSummaries = await GetMessageSummariesAsync(cancellationToken).ConfigureAwait(false);
            return messageSummaries;
        }

        /// <summary>Query the server for message IDs with matching keywords in the subject or body text.</summary>
        /// <param name="keywords">Keywords to search for.</param>
        /// <returns>List of <see cref="IMessageSummary"/> items.</returns>
        public async Task<IList<IMessageSummary>> SearchKeywordsAsync(IEnumerable<string> keywords, CancellationToken cancellationToken = default)
        {
            var subjectMatch = keywords.MatchAny(SearchQuery.SubjectContains);
            var bodyMatch = keywords.MatchAny(SearchQuery.BodyContains);
            _searchQuery = subjectMatch.Or(bodyMatch);
            var messageSummaries = await GetMessageSummariesAsync(cancellationToken).ConfigureAwait(false);
            return messageSummaries;
        }

        public IMailFolderReader Copy() => MemberwiseClone() as IMailFolderReader;

        public override string ToString() => $"{_imapReceiver} (skip {_skip}, take {_take})";

        public async ValueTask DisposeAsync() => await _imapReceiver.DisposeAsync().ConfigureAwait(false);

        public void Dispose() => _imapReceiver.Dispose();
    }
}
