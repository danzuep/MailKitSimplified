using MailKit;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MailKitSimplified.Receiver.Abstractions
{
    public interface IMailFolderMonitor
    {
        /// <summary>
        /// Idle client that monitors a mail folder for incoming messages.
        /// </summary>
        /// <param name="cancellationToken">Request cancellation token.</param>
        Task MonitorAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Idle client that monitors a mail folder for incoming messages.
        /// </summary>
        /// <param name="messageArrivalMethod">Method for processing messages as they arrive.</param>
        /// <param name="cancellationToken">Request cancellation token.</param>
        Task MonitorAsync(Func<IMessageSummary, Task> messageArrivalMethod, CancellationToken cancellationToken = default);
    }
}
