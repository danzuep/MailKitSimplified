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
    [Obsolete("Consider using MailFolderReader instead.")]
    public sealed class MailFolderClient : IMailFolderClient
    {
        public string MailFolderName => _mailFolder?.FullName ?? _imapReceiver.ToString();
        public int MailFolderCount => _mailFolder?.Count ?? 0;

        private IMailFolder _mailFolder = null;
        private readonly ILogger _logger;
        private readonly IImapReceiver _imapReceiver;

        [Obsolete("Consider using MailFolderReader instead.")]
        public MailFolderClient(IImapReceiver imapReceiver, ILogger<MailFolderClient> logger = null)
        {
            _logger = logger ?? NullLogger<MailFolderClient>.Instance;
            _imapReceiver = imapReceiver ?? throw new ArgumentNullException(nameof(imapReceiver));
        }

        [Obsolete("Consider using MailFolderReader instead.")]
        public static MailFolderClient Create(EmailReceiverOptions emailReceiverOptions, ILogger<MailFolderClient> logger = null, ILogger<ImapReceiver> logImap = null, IProtocolLogger protocolLogger = null, IImapClient imapClient = null)
        {
            var imapReceiver = ImapReceiver.Create(emailReceiverOptions, logImap, protocolLogger, imapClient);
            var mailFolderClient = new MailFolderClient(imapReceiver, logger);
            return mailFolderClient;
        }

        [Obsolete("Consider using MailFolderReader instead.")]
        public static MailFolderClient Create(IImapClient imapClient, EmailReceiverOptions emailReceiverOptions, ILogger<MailFolderClient> logger = null, ILogger<ImapReceiver> logImap = null)
        {
            var imapReceiver = ImapReceiver.Create(imapClient, emailReceiverOptions, logImap);
            var mailFolderClient = new MailFolderClient(imapReceiver, logger);
            return mailFolderClient;
        }

        [Obsolete("Consider using MailFolderReader.OpenMailFolderAsync() instead.")]
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

        /// <summary>Query just the arrival dates of messages on the server.</summary>
        /// <param name="deliveredAfter">Search for messages after this date.</param>
        /// <param name="deliveredBefore">Search for messages before this date.</param>
        /// <returns>The first 250 <see cref="UniqueId"/>s.</returns>
        [Obsolete("Consider using MailFolderReader.SearchBetweenDatesAsync() instead.")]
        public async Task<IList<UniqueId>> SearchBetweenDatesAsync(DateTime deliveredAfter, DateTime? deliveredBefore = null, CancellationToken cancellationToken = default)
        {
            DateTime before = deliveredBefore != null ? deliveredBefore.Value : DateTime.Now;
            var query = SearchQuery.DeliveredAfter(deliveredAfter).And(SearchQuery.DeliveredBefore(before));
            var uniqueIds = await SearchAsync(query, cancellationToken).ConfigureAwait(false);
            return uniqueIds;
        }

        /// <summary>Query the server for message IDs with matching keywords in the subject or body text.</summary>
        /// <param name="keywords">Keywords to search for.</param>
        /// <returns>The first 250 <see cref="UniqueId"/>s.</returns>
        [Obsolete("Consider using MailFolderReader.SearchKeywordsAsync() instead.")]
        public async Task<IList<UniqueId>> SearchKeywordsAsync(IEnumerable<string> keywords, CancellationToken cancellationToken = default)
        {
            var subjectMatch = keywords.MatchAny(SearchQuery.SubjectContains);
            var bodyMatch = keywords.MatchAny(SearchQuery.BodyContains);
            var query = subjectMatch.Or(bodyMatch);
            var uniqueIds = await SearchAsync(query, cancellationToken).ConfigureAwait(false);
            return uniqueIds;
        }

        [Obsolete("Consider using IMailReader.Query() instead.")]
        public async Task<IMessageSummary> GetNewestMessageSummaryAsync(MessageSummaryItems filter = MessageSummaryItems.UniqueId, CancellationToken cancellationToken = default)
        {
            var mailFolder = await ConnectAsync(true, cancellationToken).ConfigureAwait(false);
            var index = mailFolder.Count > 0 ? mailFolder.Count - 1 : mailFolder.Count;
            var messageSummaries = await mailFolder.FetchAsync(index, index, filter, cancellationToken).ConfigureAwait(false);
            await mailFolder.CloseAsync(false, CancellationToken.None).ConfigureAwait(false);
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
