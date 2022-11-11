using MailKit;
using MailKit.Security;
using MailKit.Net.Imap;
using MimeKit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.IO;
using System.Net;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using MailKitSimplified.Receiver.Abstractions;
using MailKitSimplified.Receiver.Models;

namespace MailKitSimplified.Receiver.Services
{
    public class MailFolderReader : IMailFolderReader
    {
        public string FolderName => _mailFolder?.FullName ?? string.Empty;
        public int FolderCount => _mailFolder?.Count ?? 0;

        private readonly ILogger _logger;
        private readonly IImapClientService _imapClientService;
        private IMailFolder _mailFolder;

        public MailFolderReader(IImapClientService imapClientService, ILogger<MailFolderReader> logger = null)
        {
            _logger = logger ?? NullLogger<MailFolderReader>.Instance;
            _imapClientService = imapClientService ?? throw new ArgumentNullException(nameof(imapClientService));
        }

        public static MailFolderReader Create(string folderName, EmailReceiverOptions emailReceiverOptions)
        {
            var imapClientService = ImapClientService.Create(emailReceiverOptions);
            var mailFolderReader = Create(folderName, imapClientService);
            return mailFolderReader;
        }

        public static MailFolderReader Create(string folderName, IImapClientService imapClientService)
        {
            var mailFolderReader = new MailFolderReader(imapClientService);
            mailFolderReader._mailFolder = imapClientService.GetFolderAsync(folderName).GetAwaiter().GetResult();
            return mailFolderReader;
        }

        public async ValueTask ReconnectAsync(bool enableWrite = false, CancellationToken cancellationToken = default)
        {
            if (_mailFolder == null)
                _mailFolder = await _imapClientService.ConnectAsync(cancellationToken).ConfigureAwait(false);
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

        public async ValueTask<IList<IMessageSummary>> GetMessageSummariesAsync(MessageSummaryItems filter, CancellationToken ct = default)
        {
            var messageSummaries = await FetchMessageSummariesAsync(0, 0, filter, ct).ConfigureAwait(false);
            return messageSummaries;
        }

        public async ValueTask<IList<IMessageSummary>> FetchMessageSummariesAsync(ushort startIndex, ushort endCount, MessageSummaryItems filter = MessageSummaryItems.UniqueId, CancellationToken ct = default)
        {
            if (startIndex > endCount)
                throw new ArgumentOutOfRangeException(nameof(startIndex));
            await ReconnectAsync(false, ct).ConfigureAwait(false);
            filter |= MessageSummaryItems.UniqueId;
            var messageSummaries = await _mailFolder.FetchAsync(startIndex, endCount - 1, filter, ct).ConfigureAwait(false);
            return messageSummaries;
        }

        public async Task<IEnumerable<IMessageSummary>> GetMessageSummariesAsync(uint from, uint to = 0, MessageSummaryItems filter = MessageSummaryItems.UniqueId, CancellationToken ct = default)
        {
            if (to < from)
                to = from;
            var messageSummaries = await FetchMessageSummariesAsync(0, 0, filter, ct).ConfigureAwait(false);
            var filteredSummaries = messageSummaries.Where(msg => msg.UniqueId.Id >= from && msg.UniqueId.Id <= to);
            return filteredSummaries;
        }

        public async Task<IEnumerable<MimeMessage>> GetRangeAsync(ushort count = 0, CancellationToken ct = default)
        {
            await ReconnectAsync(false, ct).ConfigureAwait(false);
            var mimeMessages = await GetRangeAsync(0, count, ct: ct).ConfigureAwait(false);
            return mimeMessages;
        }

        public async Task<IEnumerable<MimeMessage>> GetRangeAsync(ushort startIndex, ushort endCount, MessageSummaryItems filter = MessageSummaryItems.UniqueId, CancellationToken ct = default)
        {
            var messageSummaries = await FetchMessageSummariesAsync(startIndex, endCount, filter, ct).ConfigureAwait(false);
            var mimeMessages = await GetMimeMessagesAsync(messageSummaries, ct).ConfigureAwait(false);
            return mimeMessages;
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

        public async Task<IList<IMessageSummary>> GetMessageSummariesAsync(IEnumerable<UniqueId> uniqueIds, MessageSummaryItems filter = MessageSummaryItems.UniqueId, CancellationToken ct = default)
        {
            IList<IMessageSummary> messageSummaries = new List<IMessageSummary>();
            if (uniqueIds != null)
            {
                await ReconnectAsync(false, ct).ConfigureAwait(false);
                var orderedIds = uniqueIds.OrderBy(m => m.Id).ToList();
                messageSummaries = await _mailFolder.FetchAsync(orderedIds, filter, ct).ConfigureAwait(false);
            }
            return messageSummaries;
        }

        public async Task<MimeMessage> GetMimeMessageAsync(IMessageSummary messageSummary, CancellationToken ct = default)
        {
            MimeMessage mimeMessage = null;
            if (messageSummary?.UniqueId != null)
            {
                await ReconnectAsync(false, ct).ConfigureAwait(false);
                mimeMessage = await GetMimeMessageAsync(messageSummary.UniqueId, ct).ConfigureAwait(false);
            }
            return mimeMessage;
        }

        public async Task<IList<MimeMessage>> GetMimeMessagesAsync(IEnumerable<IMessageSummary> messageSummaries, CancellationToken ct = default)
        {
            IList<MimeMessage> mimeMessages = Array.Empty<MimeMessage>();
            if (messageSummaries != null)
            {
                var uniqueIds = messageSummaries.Select(m => m.UniqueId);
                mimeMessages = await GetMimeMessagesAsync(uniqueIds, ct).ConfigureAwait(false);
            }
            return mimeMessages;
        }

        public async Task<IList<MimeMessage>> GetMimeMessagesAsync(IEnumerable<UniqueId> uniqueIds, CancellationToken ct = default)
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

        /// <exception cref="MessageNotFoundException">Message was moved before it could be downloaded</exception>
        /// <exception cref="ImapCommandException">Message was moved before it could be downloaded</exception>
        /// <exception cref="ImapProtocolException">Message not downloaded</exception>
        /// <exception cref="IOException">Message not downloaded</exception>
        /// <exception cref="FolderNotOpenException">Message not downloaded</exception>
        /// <exception cref="InvalidOperationException">Message not downloaded</exception>
        /// <exception cref="OperationCanceledException">Message download task was cancelled.</exception>
        public async Task<MimeMessage> GetMimeMessageAsync(UniqueId uniqueId, CancellationToken ct = default)
        {
            var mimeMessage = await _mailFolder.GetMessageAsync(uniqueId, ct).ConfigureAwait(false);
            if (mimeMessage != null && _mailFolder.Access == FolderAccess.ReadWrite)
                await _mailFolder.AddFlagsAsync(uniqueId, MessageFlags.Seen, true, ct).ConfigureAwait(false);
            return mimeMessage;
        }

        public override string ToString() => $"{FolderName} ({FolderCount})";

        public virtual void Dispose()
        {
            _imapClientService?.Dispose();
            if (_mailFolder != null)
                lock (_mailFolder?.SyncRoot)
                    _mailFolder?.Close(false);
        }
    }
}
