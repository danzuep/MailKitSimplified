using MimeKit;
using MailKit;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using MailKitSimplified.Receiver.Abstractions;

namespace MailKitSimplified.Receiver.Services
{
    public sealed class MailFolderReader : MailReader, IMailFolderReader
    {
        public MailFolderReader(IImapReceiver imapReceiver) : base(imapReceiver) { }

        public new static MailFolderReader Create(IImapReceiver imapReceiver, string mailFolderName)
        {
            var emailReader = new MailFolderReader(imapReceiver);
            emailReader.ReadFrom(mailFolderName);
            return emailReader;
        }

        private MailFolderReader ReadFrom(string mailFolderName)
        {
            if (string.IsNullOrWhiteSpace(mailFolderName))
                throw new ArgumentNullException(nameof(mailFolderName));
            _mailFolderName = mailFolderName;
            return this;
        }

        public async ValueTask<IEnumerable<IMessageSummary>> GetMessageSummariesAsync(IEnumerable<UniqueId> uniqueIds, MessageSummaryItems filter = MessageSummaryItems.UniqueId, CancellationToken cancellationToken = default)
        {
            IEnumerable<IMessageSummary> filteredResults = Array.Empty<IMessageSummary>();
            filter |= MessageSummaryItems.UniqueId;
            var orderedIds = uniqueIds.OrderBy(m => m.Id).ToList();
            using (var mailFolderClient = await _imapReceiver.ConnectMailFolderClientAsync(_mailFolderName, cancellationToken).ConfigureAwait(false))
            {
                var mailFolder = await mailFolderClient.ConnectAsync(false, cancellationToken).ConfigureAwait(false);
                var messageSummaries = await mailFolder.FetchAsync(orderedIds, filter, cancellationToken).ConfigureAwait(false);
                filteredResults = messageSummaries.Where(m => uniqueIds.Contains(m.UniqueId));
            }
            return filteredResults;
        }

        public async ValueTask<MimeMessage> GetMimeMessageAsync(UniqueId uniqueId, CancellationToken cancellationToken = default)
        {
            MimeMessage mimeMessage;
            using (var mailFolderClient = await _imapReceiver.ConnectMailFolderClientAsync(_mailFolderName, cancellationToken).ConfigureAwait(false))
            {
                var mailFolder = await mailFolderClient.ConnectAsync(false, cancellationToken).ConfigureAwait(false);
                mimeMessage = await mailFolder.GetMessageAsync(uniqueId, cancellationToken).ConfigureAwait(false);
            }
            return mimeMessage;
        }

        public async ValueTask<IList<MimeMessage>> GetMimeMessagesAsync(IEnumerable<UniqueId> uniqueIds, CancellationToken cancellationToken = default)
        {
            IList<MimeMessage> mimeMessages = new List<MimeMessage>();
            if (uniqueIds != null)
            {
                using (var mailFolderClient = await _imapReceiver.ConnectMailFolderClientAsync(_mailFolderName, cancellationToken).ConfigureAwait(false))
                {
                    var mailFolder = await mailFolderClient.ConnectAsync(false, cancellationToken).ConfigureAwait(false);
                    foreach (var uniqueId in uniqueIds)
                    {
                        var mimeMessage = await GetMimeMessageAsync(uniqueId, cancellationToken).ConfigureAwait(false);
                        mimeMessages.Add(mimeMessage);
                        if (cancellationToken.IsCancellationRequested)
                            break;
                    }
                }
            }
            return mimeMessages;
        }

        public override string ToString() => _imapReceiver.ToString();
    }
}
