using MimeKit;
using MailKit;
using MailKit.Net.Imap;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MailKitSimplified.Receiver.Abstractions;

namespace MailKitSimplified.Receiver.Services
{
    public class MailFolderReader : IMailFolderReader
    {
        public string FolderName => _mailFolder.FullName;
        public int FolderCount => _mailFolder.Count;

        private readonly ILogger _logger;
        private readonly IMailFolder _mailFolder;

        public MailFolderReader(IMailFolder mailFolder, ILogger<MailFolderReader> logger = null)
        {
            _logger = logger ?? NullLogger<MailFolderReader>.Instance;
            _mailFolder = mailFolder ?? throw new ArgumentNullException(nameof(mailFolder));
        }

        public async ValueTask ReconnectAsync(bool enableWrite = false, CancellationToken cancellationToken = default)
        {
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
        }

        public async ValueTask<IEnumerable<IMessageSummary>> GetMessageSummariesAsync(IEnumerable<UniqueId> uniqueIds, MessageSummaryItems filter = MessageSummaryItems.UniqueId, CancellationToken cancellationToken = default)
        {
            bool closeWhenFinished = !_mailFolder?.IsOpen ?? true;
            await ReconnectAsync(false, cancellationToken).ConfigureAwait(false);
            filter |= MessageSummaryItems.UniqueId;
            _ = await _mailFolder.OpenAsync(FolderAccess.ReadOnly, cancellationToken).ConfigureAwait(false);
            var orderedIds = uniqueIds.OrderBy(m => m.Id).ToList();
            var messageSummaries = await _mailFolder.FetchAsync(orderedIds, filter, cancellationToken).ConfigureAwait(false);
            if (closeWhenFinished)
                await _mailFolder.CloseAsync().ConfigureAwait(false);
            return messageSummaries;
        }

        public async ValueTask<MimeMessage> GetMessageAsync(ushort index = 0, CancellationToken ct = default)
        {
            bool closeWhenFinished = !_mailFolder?.IsOpen ?? true;
            await ReconnectAsync(false, ct).ConfigureAwait(false);
            if (closeWhenFinished)
            {
                //_ = _mailFolder.OpenAsync(FolderAccess.ReadOnly, ct).ConfigureAwait(false);
                _logger.LogTrace($"{this} mail folder briefly opened with read-only access to get message at index {index}.");
            }
            var mimeMessage = await _mailFolder.GetMessageAsync(index, ct).ConfigureAwait(false);
            if (closeWhenFinished)
                await _mailFolder.CloseAsync(false, ct).ConfigureAwait(false);
            return mimeMessage;
        }

        public async ValueTask<MimeMessage> GetMimeMessageAsync(IMessageSummary messageSummary, CancellationToken ct = default)
        {
            MimeMessage mimeMessage = null;
            if (messageSummary?.UniqueId != null)
            {
                await ReconnectAsync(false, ct).ConfigureAwait(false);
                mimeMessage = await GetMimeMessageAsync(messageSummary.UniqueId, ct).ConfigureAwait(false);
            }
            return mimeMessage;
        }

        public async ValueTask<IList<MimeMessage>> GetMimeMessagesAsync(IEnumerable<IMessageSummary> messageSummaries, CancellationToken ct = default)
        {
            IList<MimeMessage> mimeMessages = Array.Empty<MimeMessage>();
            if (messageSummaries != null)
            {
                var uniqueIds = messageSummaries.Select(m => m.UniqueId);
                mimeMessages = await GetMimeMessagesAsync(uniqueIds, ct).ConfigureAwait(false);
            }
            return mimeMessages;
        }

        public async ValueTask<IList<MimeMessage>> GetMimeMessagesAsync(IEnumerable<UniqueId> uniqueIds, CancellationToken ct = default)
        {
            IList<MimeMessage> mimeMessages = new List<MimeMessage>();
            if (uniqueIds != null)
            {
                await ReconnectAsync(false, ct).ConfigureAwait(false);
                foreach (var uniqueId in uniqueIds.OrderBy(id => id.Id))
                {
                    var mimeMessage = await GetMimeMessageAsync(uniqueId, ct).ConfigureAwait(false);
                    mimeMessages.Add(mimeMessage);
                    if (ct.IsCancellationRequested) break;
                }
            }
            return mimeMessages;
        }

        //public async ValueTask<IList<MimeMessage>> GetMimeMessagesAsync(IEnumerable<UniqueId> uniqueIds, CancellationToken cancellationToken = default)
        //{
        //    bool closeWhenFinished = !_mailFolder?.IsOpen ?? true;
        //    await ReconnectAsync(false, cancellationToken).ConfigureAwait(false);
        //    var mimeMessages = new List<MimeMessage>();
        //    var orderedIds = uniqueIds.OrderBy(m => m.Id).ToList();
        //    var messageSummaries = await _mailFolder.FetchAsync(orderedIds, MessageSummaryItems.UniqueId, cancellationToken).ConfigureAwait(false);
        //    foreach (var uniqueId in messageSummaries.Select(m => m.UniqueId))
        //    {
        //        var mimeMessage = await _mailFolder.GetMessageAsync(uniqueId, cancellationToken).ConfigureAwait(false);
        //        mimeMessages.Add(mimeMessage);
        //    }
        //    if (closeWhenFinished)
        //        await _mailFolder.CloseAsync().ConfigureAwait(false);
        //    return mimeMessages;
        //}

        /// <exception cref="MessageNotFoundException">Message was moved before it could be downloaded</exception>
        /// <exception cref="ImapCommandException">Message was moved before it could be downloaded</exception>
        /// <exception cref="ImapProtocolException">Message not downloaded</exception>
        /// <exception cref="IOException">Message not downloaded</exception>
        /// <exception cref="FolderNotOpenException">Message not downloaded</exception>
        /// <exception cref="InvalidOperationException">Message not downloaded</exception>
        /// <exception cref="OperationCanceledException">Message download task was cancelled.</exception>
        public async ValueTask<MimeMessage> GetMimeMessageAsync(UniqueId uniqueId, CancellationToken ct = default)
        {
            var mimeMessage = await _mailFolder.GetMessageAsync(uniqueId, ct).ConfigureAwait(false);
            if (mimeMessage != null && _mailFolder.Access == FolderAccess.ReadWrite)
                await _mailFolder.AddFlagsAsync(uniqueId, MessageFlags.Seen, true, ct).ConfigureAwait(false);
            return mimeMessage;
        }

        public override string ToString() => $"{FolderName} ({FolderCount})";

        public virtual void Dispose()
        {
            if (_mailFolder != null && _mailFolder.IsOpen)
                lock (_mailFolder.SyncRoot)
                    _mailFolder.Close(false);
        }
    }
}
