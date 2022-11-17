using MimeKit;
using MailKit;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using MailKitSimplified.Receiver.Abstractions;
using MailKitSimplified.Receiver.Models;

namespace MailKitSimplified.Receiver.Services
{
    public class MailReader : IMailReader
    {
        public static readonly MessageSummaryItems ItemFilter =
            MessageSummaryItems.Envelope |
            MessageSummaryItems.BodyStructure |
            MessageSummaryItems.UniqueId;

        private readonly string _mailFolderName;
        private int _skip = 0;
        private bool _continueSkip = false;
        private int _take = 250;

        private readonly IImapReceiver _imapReceiver;

        public MailReader(IImapReceiver imapReceiver, string mailFolderName = "INBOX")
        {
            _imapReceiver = imapReceiver ?? throw new ArgumentNullException(nameof(imapReceiver));
            _mailFolderName = mailFolderName ?? throw new ArgumentNullException(nameof(mailFolderName));
        }

        public static MailReader Create(string mailFolderName, EmailReceiverOptions emailReceiverOptions)
        {
            var imapReceiver = ImapReceiver.Create(emailReceiverOptions);
            var emailReader = new MailReader(imapReceiver, mailFolderName);
            return emailReader;
        }

        public IMailReader Skip(int skipCount, bool continuous = false)
        {
            if (skipCount < 0)
                throw new ArgumentOutOfRangeException(nameof(skipCount));
            _skip = skipCount;
            _continueSkip = continuous;
            return this;
        }
        
        public IMailReader Take(int takeCount)
        {
            if (takeCount < 0)
                throw new ArgumentOutOfRangeException(nameof(takeCount));
            _take = takeCount;
            return this;
        }

        private async ValueTask<IList<IMessageSummary>> GetMessageSummariesAsync(IMailFolder mailFolder, MessageSummaryItems filter = MessageSummaryItems.UniqueId, CancellationToken cancellationToken = default)
        {
            int startIndex = _skip < mailFolder.Count ? _skip : mailFolder.Count;
            int endIndex = _take > 0 ? startIndex + _take > 0 ? _take - 1 + startIndex : mailFolder.Count : startIndex;
            var messageSummaries = await mailFolder.FetchAsync(startIndex, endIndex, filter, cancellationToken).ConfigureAwait(false);
            if (_continueSkip)
                _skip = endIndex + 1;
            return messageSummaries;
        }

        public async ValueTask<IList<IMessageSummary>> GetMessageSummariesAsync(MessageSummaryItems filter, CancellationToken cancellationToken = default)
        {
            var mailFolder = await _imapReceiver.GetFolderAsync(_mailFolderName, cancellationToken).ConfigureAwait(false);
            _ = await mailFolder.OpenAsync(FolderAccess.ReadOnly, cancellationToken).ConfigureAwait(false);
            var messageSummaries = await GetMessageSummariesAsync(mailFolder, filter, cancellationToken).ConfigureAwait(false);
            await mailFolder.CloseAsync().ConfigureAwait(false);
            return messageSummaries;
        }

        public async ValueTask<IList<IMessageSummary>> GetMessageDatesAsync(CancellationToken cancellationToken = default) =>
            await GetMessageSummariesAsync(MessageSummaryItems.InternalDate, cancellationToken).ConfigureAwait(false);

        public async ValueTask<IList<IMessageSummary>> GetMessageEnvelopeAsync(CancellationToken cancellationToken = default) =>
            await GetMessageSummariesAsync(MessageSummaryItems.Envelope, cancellationToken).ConfigureAwait(false);

        public async ValueTask<IList<IMessageSummary>> GetMessageSummariesAsync(CancellationToken cancellationToken = default) =>
            await GetMessageSummariesAsync(ItemFilter, cancellationToken).ConfigureAwait(false);

        public async ValueTask<IList<UniqueId>> GetUniqueIdsAsync(CancellationToken cancellationToken = default)
        {
            var messageSummaries = await GetMessageSummariesAsync(MessageSummaryItems.UniqueId, cancellationToken).ConfigureAwait(false);
            var uniqueIds = messageSummaries.Select(m => m.UniqueId).OrderBy(m => m.Id).ToList();
            return uniqueIds;
        }

        public async ValueTask<IList<MimeMessage>> GetMimeMessagesAsync(CancellationToken cancellationToken = default, ITransferProgress transferProgress = null)
        {
            var mimeMessages = new List<MimeMessage>();
            var mailFolder = await _imapReceiver.GetFolderAsync(_mailFolderName, cancellationToken).ConfigureAwait(false);
            _ = await mailFolder.OpenAsync(FolderAccess.ReadOnly, cancellationToken).ConfigureAwait(false);
            int startIndex = _skip < mailFolder.Count ? _skip : mailFolder.Count;
            int endIndex = startIndex + _take > mailFolder.Count ? mailFolder.Count : startIndex + _take;
            for (int index = startIndex; index < endIndex; index++)
            {
                var mimeMessage = await mailFolder.GetMessageAsync(index, cancellationToken, transferProgress).ConfigureAwait(false);
                mimeMessages.Add(mimeMessage);
            }
            await mailFolder.CloseAsync().ConfigureAwait(false);
            return mimeMessages;
        }

        public override string ToString() => $"{_mailFolderName} (skip {_skip}, take {_take})";
    }
}
