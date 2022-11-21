using MimeKit;
using MailKit;
using MailKit.Net.Imap;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using MailKitSimplified.Receiver.Abstractions;

namespace MailKitSimplified.Receiver.Services
{
    public class MailFolderReader : IMailFolderReader
    {
        private readonly IMailFolderClient _mailFolderClient;

        public MailFolderReader(IMailFolderClient mailFolderClient) =>
            _mailFolderClient = mailFolderClient ?? throw new ArgumentNullException(nameof(mailFolderClient));

        public ValueTask<IMailFolder> ReconnectAsync(CancellationToken cancellationToken = default) =>
            ReconnectAsync(false, cancellationToken);

        public ValueTask<IMailFolder> ReconnectAsync(bool enableWrite, CancellationToken cancellationToken = default) =>
            _mailFolderClient.ConnectAsync(enableWrite, cancellationToken);

        public async ValueTask<IEnumerable<IMessageSummary>> GetMessageSummariesAsync(IEnumerable<UniqueId> uniqueIds, MessageSummaryItems filter = MessageSummaryItems.UniqueId, CancellationToken cancellationToken = default)
        {
            var mailFolder = await ReconnectAsync(cancellationToken).ConfigureAwait(false);
            filter |= MessageSummaryItems.UniqueId;
            var orderedIds = uniqueIds.OrderBy(m => m.Id).ToList();
            var messageSummaries = await mailFolder.FetchAsync(orderedIds, filter, cancellationToken).ConfigureAwait(false);
            var filteredResults = messageSummaries.Where(m => uniqueIds.Contains(m.UniqueId));
            return filteredResults;
        }

        public async ValueTask<MimeMessage> GetMimeMessageAsync(ushort index = 0, CancellationToken cancellationToken = default)
        {
            var mailFolder = await ReconnectAsync(cancellationToken).ConfigureAwait(false);
            var mimeMessage = await mailFolder.GetMessageAsync(index, cancellationToken).ConfigureAwait(false);
            return mimeMessage;
        }

        /// <exception cref="MessageNotFoundException">Message was moved before it could be downloaded</exception>
        /// <exception cref="ImapCommandException">Message was moved before it could be downloaded</exception>
        /// <exception cref="FolderNotOpenException">Mail folder was closed</exception>
        /// <exception cref="IOException">Message not downloaded</exception>
        /// <exception cref="ImapProtocolException">Message not downloaded</exception>
        /// <exception cref="InvalidOperationException">Message not downloaded</exception>
        /// <exception cref="OperationCanceledException">Message download task was cancelled.</exception>
        public async ValueTask<MimeMessage> GetMimeMessageAsync(UniqueId uniqueId, CancellationToken cancellationToken = default)
        {
            var mailFolder = await ReconnectAsync(cancellationToken).ConfigureAwait(false);
            var mimeMessage = await mailFolder.GetMessageAsync(uniqueId, cancellationToken).ConfigureAwait(false);
            return mimeMessage;
        }

        public async ValueTask<IList<MimeMessage>> GetMimeMessagesAsync(IEnumerable<UniqueId> uniqueIds, CancellationToken cancellationToken = default)
        {
            IList<MimeMessage> mimeMessages = new List<MimeMessage>();
            if (uniqueIds != null)
            {
                await ReconnectAsync(false, cancellationToken).ConfigureAwait(false);
                foreach (var uniqueId in uniqueIds)
                {
                    var mimeMessage = await GetMimeMessageAsync(uniqueId, cancellationToken).ConfigureAwait(false);
                    mimeMessages.Add(mimeMessage);
                    if (cancellationToken.IsCancellationRequested)
                        break;
                }
            }
            return mimeMessages;
        }

        public async ValueTask<IList<MimeMessage>> GetMimeMessagesAsync(IEnumerable<IMessageSummary> messageSummaries, CancellationToken cancellationToken = default)
        {
            var uniqueIds = messageSummaries?.Select(m => m.UniqueId);
            var mimeMessages = await GetMimeMessagesAsync(uniqueIds, cancellationToken).ConfigureAwait(false);
            return mimeMessages;
        }

        public override string ToString() => _mailFolderClient.ToString();

        public ValueTask DisposeAsync() => _mailFolderClient.DisposeAsync();

        public void Dispose() => _mailFolderClient.Dispose();
    }
}
