using MailKit;
using MailKitSimplified.Receiver.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MailKitSimplified.Receiver.Abstractions
{
    public interface IMailFolderMonitor
    {
        /// <summary>
        /// Specify length of time to idle for, default is 9 minutes.
        /// </summary>
        /// <param name="idleMinutes"></param>
        /// <returns><see cref="IMailFolderMonitor"/> with <see cref="FolderMonitorOptions.IdleMinutes"/> configured.</returns>
        IMailFolderMonitor SetIdleMinutes(byte idleMinutes = FolderMonitorOptions.IdleMinutesImap);

        /// <summary>
        /// Specify number of times to retry on failure, default is 3 times.
        /// </summary>
        /// <param name="maxRetries"></param>
        /// <returns><see cref="IMailFolderMonitor"/> with <see cref="FolderMonitorOptions.MaxRetries"/> configured.</returns>
        IMailFolderMonitor SetMaxRetries(byte maxRetries = 1);

        /// <summary>
        /// Ignore existing messages, processing emails on connect is enabled by default.
        /// </summary>
        /// <param name="ignoreExisting">Whether to ignore existing emails or not.</param>
        /// <returns><see cref="IMailFolderMonitor"/> that will ignore existing emails.</returns>
        IMailFolderMonitor SetIgnoreExistingMailOnConnect(bool ignoreExisting = true);

        /// <summary>
        /// Specify which properties of <see cref="IMessageSummary"/> should be populated other than <see cref="UniqueId"/>.
        /// </summary>
        /// <param name="itemSelection">Message summary items to include.</param>
        /// <returns><see cref="IMailFolderMonitor"/> with <see cref="MessageSummaryItems"/> configured.</returns>
        IMailFolderMonitor SetMessageSummaryItems(MessageSummaryItems itemSelection = MessageSummaryItems.Envelope);

        /// <summary>
        /// Asynchronous function for processing messages as they are added to the mail folder.
        /// </summary>
        IMailFolderMonitor OnMessageArrival(Func<IMessageSummary, Task> messageArrivalMethod);

        /// <summary>
        /// Asynchronous function for processing messages as they are removed from the mail folder.
        /// </summary>
        IMailFolderMonitor OnMessageDeparture(Func<IMessageSummary, Task> messageDepartureMethod);

        /// <summary>
        /// Synchronous action for processing messages as they are added to the mail folder.
        /// </summary>
        IMailFolderMonitor OnMessageArrival(Action<IMessageSummary> messageArrivalMethod);

        /// <summary>
        /// Synchronous action for processing messages as they are removed from the mail folder.
        /// </summary>
        IMailFolderMonitor OnMessageDeparture(Action<IMessageSummary> messageDepartureMethod);

        /// <summary>
        /// Idle client that monitors a mail folder for incoming messages.
        /// </summary>
        /// <param name="cancellationToken">Request cancellation token.</param>
        Task IdleAsync(CancellationToken cancellationToken = default);
    }
}
