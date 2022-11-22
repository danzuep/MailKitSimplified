using MimeKit;
using MailKit;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using MailKitSimplified.Receiver.Abstractions;

namespace MailKitSimplified.Receiver.Services
{
    public class MailReader : IMailReader
    {
        public static readonly MessageSummaryItems CoreMessageItems =
            MessageSummaryItems.Envelope |
            MessageSummaryItems.BodyStructure |
            MessageSummaryItems.UniqueId;

        protected string _mailFolderName = null;
        private int _skip = 0;
        private bool _continueSkip = false;
        private int _take = 250;

        protected readonly IImapReceiver _imapReceiver;

        public MailReader(IImapReceiver imapReceiver)
        {
            _imapReceiver = imapReceiver ?? throw new ArgumentNullException(nameof(imapReceiver));
        }

        public static MailReader Create(IImapReceiver imapReceiver, string mailFolderName)
        {
            var emailReader = new MailReader(imapReceiver);
            emailReader.ReadFrom(mailFolderName);
            return emailReader;
        }

        private MailReader ReadFrom(string mailFolderName)
        {
            if (string.IsNullOrWhiteSpace(mailFolderName))
                throw new ArgumentNullException(nameof(mailFolderName));
            _mailFolderName = mailFolderName;
            return this;
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
            filter |= MessageSummaryItems.UniqueId;
            var mailFolder = await _imapReceiver.ConnectMailFolderAsync(_mailFolderName, cancellationToken).ConfigureAwait(false);
            _ = await mailFolder.OpenAsync(FolderAccess.ReadOnly, cancellationToken).ConfigureAwait(false);
            var messageSummaries = await GetMessageSummariesAsync(mailFolder, filter, cancellationToken).ConfigureAwait(false);
            await mailFolder.CloseAsync().ConfigureAwait(false);
            return messageSummaries;
        }

        public async ValueTask<IList<IMessageSummary>> GetMessageSummariesAsync(CancellationToken cancellationToken = default) =>
            await GetMessageSummariesAsync(CoreMessageItems, cancellationToken).ConfigureAwait(false);

        public async ValueTask<IList<MimeMessage>> GetMimeMessagesAsync(CancellationToken cancellationToken = default, ITransferProgress transferProgress = null)
        {
            var mimeMessages = new List<MimeMessage>();
            var mailFolder = await _imapReceiver.ConnectMailFolderAsync(_mailFolderName, cancellationToken).ConfigureAwait(false);
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
