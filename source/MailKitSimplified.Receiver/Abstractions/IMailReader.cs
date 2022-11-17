using MimeKit;
using MailKit;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace MailKitSimplified.Receiver.Abstractions
{
    public interface IMailReader
    {
        IMailReader Skip(int skipCount, bool continuous = false);

        IMailReader Take(int takeCount);

        ValueTask<IList<IMessageSummary>> GetMessageSummariesAsync(MessageSummaryItems filter, CancellationToken cancellationToken = default);

        ValueTask<IList<IMessageSummary>> GetMessageSummariesAsync(CancellationToken cancellationToken = default);

        ValueTask<IList<MimeMessage>> GetMimeMessagesAsync(CancellationToken cancellationToken = default, ITransferProgress transferProgress = null);
    }
}
