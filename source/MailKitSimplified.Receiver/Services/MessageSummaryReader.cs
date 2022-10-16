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

namespace MailKitSimplified.Receiver.Services
{
    public class MessageSummaryReader : IMessageSummaryReader
    {
        public static readonly MessageSummaryItems ItemFilter =
            MessageSummaryItems.Envelope |
            MessageSummaryItems.BodyStructure |
            MessageSummaryItems.UniqueId;

        private readonly ILogger _logger;
        private readonly IMailFolderReader _mailFolderReader;

        public MessageSummaryReader(IMailFolderReader mailFolderReader, ILogger<MessageSummaryReader> logger = null)
        {
            _logger = logger ?? NullLogger<MessageSummaryReader>.Instance;
            _mailFolderReader = mailFolderReader ?? throw new ArgumentNullException(nameof(mailFolderReader));
        }

        public async ValueTask<IList<IMessageSummary>> GetMessageIdsAsync(CancellationToken ct = default) =>
            await _mailFolderReader.GetMessageSummariesAsync(MessageSummaryItems.UniqueId, ct).ConfigureAwait(false);

        public async ValueTask<IList<IMessageSummary>> GetMessageDatesAsync(CancellationToken ct = default) =>
            await _mailFolderReader.GetMessageSummariesAsync(MessageSummaryItems.InternalDate, ct).ConfigureAwait(false);

        public async ValueTask<IList<IMessageSummary>> GetMessageEnvelopeAsync(CancellationToken ct = default) =>
            await _mailFolderReader.GetMessageSummariesAsync(MessageSummaryItems.Envelope, ct).ConfigureAwait(false);

        public async ValueTask<IList<IMessageSummary>> GetMessageSummariesAsync(CancellationToken ct = default) =>
            await _mailFolderReader.GetMessageSummariesAsync(ItemFilter, ct).ConfigureAwait(false);

        public void Dispose() => _mailFolderReader.Dispose();
    }
}
