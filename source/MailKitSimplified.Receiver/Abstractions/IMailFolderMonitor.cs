using MailKit;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MailKitSimplified.Receiver.Abstractions
{
    public interface IMailFolderMonitor
    {
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
        /// Asynchronous function for processing messages as flags change.
        /// </summary>
        IMailFolderMonitor OnMessageFlagsChanged(Func<IMessageSummary, Task> messageFlagsChangedMethod);

        /// <summary>
        /// Asynchronous function for processing messages as they are removed from the mail folder.
        /// </summary>
        IMailFolderMonitor OnMessageDeparture(Func<IMessageSummary, Task> messageDepartureMethod);

        /// <summary>
        /// Synchronous action for processing messages as they are added to the mail folder.
        /// </summary>
        IMailFolderMonitor OnMessageArrival(Action<IMessageSummary> messageArrivalMethod);

        /// <summary>
        /// Synchronous action for processing messages as flags change.
        /// </summary>
        IMailFolderMonitor OnMessageFlagsChanged(Action<IMessageSummary> messageFlagsChangedMethod);

        /// <summary>
        /// Synchronous action for processing messages as they are removed from the mail folder.
        /// </summary>
        IMailFolderMonitor OnMessageDeparture(Action<IMessageSummary> messageDepartureMethod);

        /// <summary>
        /// Idle client that monitors a mail folder for incoming messages.
        /// </summary>
        /// <param name="cancellationToken">Request cancellation token.</param>
        /// <param name="handleExceptions">Option to disable exception handling.</param>
        Task IdleAsync(CancellationToken cancellationToken = default, bool handleExceptions = true);
    }
}
