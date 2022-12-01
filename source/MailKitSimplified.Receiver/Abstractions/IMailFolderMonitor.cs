using MailKit;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MailKitSimplified.Receiver.Abstractions
{
    public interface IMailFolderMonitor
    {
        /// <summary>
        /// Method for processing messages as they are added to the mail folder.
        /// </summary>
        Func<IMessageSummary, Task> MessageArrivalMethod { set; }

        /// <summary>
        /// Method for processing messages as they are removed from the mail folder.
        /// </summary>
        Func<IMessageSummary, Task> MessageDepartureMethod { set; }

        /// <summary>
        /// Idle client that monitors a mail folder for incoming messages.
        /// </summary>
        /// <param name="cancellationToken">Request cancellation token.</param>
        Task IdleAsync(CancellationToken cancellationToken = default);
    }
}
