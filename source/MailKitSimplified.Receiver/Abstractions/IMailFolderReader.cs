using MailKit;
using MimeKit;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace MailKitSimplified.Receiver.Abstractions
{
    public interface IMailFolderReader : IAsyncDisposable, IDisposable
    {
        ValueTask<IMailFolder> ReconnectAsync(bool enableWrite = false, CancellationToken cancellationToken = default);

        ValueTask<IEnumerable<IMessageSummary>> GetMessageSummariesAsync(IEnumerable<UniqueId> uniqueIds, MessageSummaryItems filter = MessageSummaryItems.UniqueId, CancellationToken cancellationToken = default);

        ValueTask<MimeMessage> GetMimeMessageAsync(UniqueId uniqueId, CancellationToken cancellationToken = default);

        ValueTask<IList<MimeMessage>> GetMimeMessagesAsync(IEnumerable<UniqueId> uniqueIds, CancellationToken cancellationToken = default);
    }
}
