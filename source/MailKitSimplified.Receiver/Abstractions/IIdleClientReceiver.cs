using System.Threading;
using System.Threading.Tasks;

namespace MailKitSimplified.Receiver.Abstractions
{
    public interface IIdleClientReceiver
    {
        /// <summary>
        /// Idle client that monitors a mail folder for incoming messages.
        /// </summary>
        /// <param name="messagesArrivedMethod">Method for processing messages as they arrive.</param>
        /// <param name="cancellationToken">Request cancellation token.</param>
        Task MonitorAsync(MessagesArrived messagesArrivedMethod, CancellationToken cancellationToken = default);
    }
}
