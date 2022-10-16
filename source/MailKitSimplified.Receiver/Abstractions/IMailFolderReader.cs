using MailKit;
using MimeKit;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace MailKitSimplified.Receiver.Abstractions
{
    public interface IMailFolderReader : IDisposable
    {
        ValueTask ReconnectAsync(bool enableWrite = false, CancellationToken cancellationToken = default);
        ValueTask<IList<IMessageSummary>> GetMessageSummariesAsync(MessageSummaryItems filter, CancellationToken cancellationToken = default);
        Task<MimeMessage> GetMimeMessageAsync(UniqueId uniqueId, CancellationToken cancellationToken = default);
        Task<IList<MimeMessage>> GetMimeMessagesAsync(IEnumerable<UniqueId> uniqueIds, CancellationToken cancellationToken = default);
    }
}
