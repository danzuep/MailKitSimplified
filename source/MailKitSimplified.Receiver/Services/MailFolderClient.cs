using MailKit;
using MailKit.Search;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MailKitSimplified.Receiver.Abstractions;
using MailKitSimplified.Receiver.Extensions;

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
        public async Task<IList<UniqueId>> SearchKeywordsAsync(IEnumerable<string> keywords, CancellationToken cancellationToken = default)
        {
            var subjectQuery = keywords.EnumerateOr(SearchQuery.SubjectContains);
            var bodyQuery = keywords.EnumerateOr(SearchQuery.BodyContains);
            var query = subjectQuery.Or(bodyQuery);
            var uniqueIds = await SearchAsync(query, cancellationToken).ConfigureAwait(false);
            return uniqueIds;
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
