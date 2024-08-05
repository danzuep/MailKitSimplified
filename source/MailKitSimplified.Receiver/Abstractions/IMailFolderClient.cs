using MimeKit;
using MailKit;
using MailKit.Search;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MailKitSimplified.Receiver.Abstractions
{
    public interface IMailFolderClient : IAsyncDisposable, IDisposable
    {
        /// <summary>Mail folder that contains sent messages.</summary>
        IMailFolder SentFolder { get; }

        /// <summary>Mail folder that contains message drafts.</summary>
        IMailFolder DraftsFolder { get; }

        /// <summary>Mail folder that contains spam messages.</summary>
        IMailFolder JunkFolder { get; }

        /// <summary>Mail folder that contains deleted messages.</summary>
        IMailFolder TrashFolder { get; }

        /// <summary>
        /// Connect to the configured mail folder.
        /// </summary>
        /// <param name="enableWrite">Optionally enable ReadWrite access.</param>
        /// <param name="cancellationToken">Mail folder connection cancellation token.</param>
        /// <returns>Connected <see cref="IMailFolder"/>.</returns>
        ValueTask<IMailFolder> ConnectAsync(bool enableWrite = false, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get or create a mail folder in the user namespace.
        /// </summary>
        /// <param name="mailFolderFullName">Folder name to search for.</param>
        /// <param name="createIfNotFound">Option to create a new folder if no existing folder is found.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Mail folder with a matching name.</returns>
        Task<IMailFolder> GetFolderAsync(string mailFolderFullName, bool createIfNotFound = false, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get a mail folder from a list of possible names.
        /// </summary>
        /// <param name="folderNames">Folder names to search for.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Mail folder with a matching name.</returns>
        Task<IMailFolder> GetFolderAsync(IEnumerable<string> folderNames = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Add flags with checks to make sure the folder is open and writeable.
        /// If there's a delete flag then it calls the Expunge method.
        /// </summary>
        /// <param name="uniqueIds">UniqueIDs to apply the flags to.</param>
        /// <param name="messageFlags"><see cref="MessageFlags"/> to add.</param>
        /// <param name="silent">Does not emit an <see cref="IMailFolder.MessageFlagsChanged"/> event if set.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task<int> AddFlagsAsync(IEnumerable<UniqueId> uniqueIds, MessageFlags messageFlags, bool silent = true, CancellationToken cancellationToken = default);

        /// <summary>
        /// Continues to search the mailbox and add the specified flags until the query stops returning results.
        /// If there's a delete flag then it calls the Expunge method after every set of results.
        /// </summary>
        /// <param name="searchQuery"><see cref="SearchQuery"/> to apply.</param>
        /// <param name="messageFlags"><see cref="MessageFlags"/> to add.</param>
        /// <param name="silent">Does not emit an <see cref="IMailFolder.MessageFlagsChanged"/> event if set.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task<int> AddFlagsAsync(SearchQuery searchQuery, MessageFlags messageFlags, bool silent = true, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously append the specified message to the folder and return the UniqueId assigned to the message.
        /// </summary>
        /// <param name="message"><see cref="MimeMessage"/> to append</param>
        /// <param name="messageFlags"><see cref="MessageFlags"/> to add.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <param name="transferProgress">Current email appending progress</param>
        /// <returns><see cref="UniqueId"/> of the message.</returns>
        Task<UniqueId?> AppendSentMessageAsync(MimeMessage message, MessageFlags messageFlags = MessageFlags.Seen, CancellationToken cancellationToken = default, ITransferProgress transferProgress = null);

        /// <summary>
        /// Add flags with checks to make sure the folder is open and writeable.
        /// If there's a delete flag then it calls the Expunge method.
        /// </summary>
        /// <param name="messageSummaries">UniqueIDs to apply the flags to.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task<int> DeleteMessagesAsync(IEnumerable<IMessageSummary> messageSummaries, CancellationToken cancellationToken = default);

        /// <summary>
        /// Delete (filtered) messages delivered before a time relative to now.
        /// If the offset is measured in days then it will measure from midnight local time.
        /// </summary>
        /// <param name="relativeOffset">Time relative to now, e.g. TimeSpan.FromDays(28)</param>
        /// <param name="filter">Optional additional filter, e.g. SearchQuery.Seen</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task<int> DeleteMessagesAsync(TimeSpan relativeOffset, SearchQuery filter = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously copy the specified message to the specified folder.
        /// </summary>
        /// <param name="messageSummary">Source <see cref="IMailFolder"/> and <see cref="UniqueId"/>.</param>
        /// <param name="mailFolder">Destination <see cref="IMailFolder"/></param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns><see cref="UniqueId"/> of the moved message.</returns>
        Task<UniqueId?> CopyToAsync(IMessageSummary messageSummary, SpecialFolder mailFolder = SpecialFolder.Drafts, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously move the specified message to the destination folder.
        /// </summary>
        /// <param name="messageUid"><see cref="UniqueId"/> to move.</param>
        /// <param name="destination">Destination mail folder.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns><see cref="UniqueId"/> in the <see cref="IMailFolder"/> destination, or null.</returns>
        Task<UniqueId?> MoveToAsync(UniqueId messageUid, IMailFolder destination, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously move the specified message to the destination folder.
        /// </summary>
        /// <param name="messageUid"><see cref="UniqueId"/> to move.</param>
        /// <param name="destinationFolder">Name of the destination mail folder.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns><see cref="UniqueId"/> in the <see cref="IMailFolder"/> destination, or null.</returns>
        Task<UniqueId?> MoveToAsync(UniqueId messageUid, string destinationFolder, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously move the specified message to the specified folder.
        /// </summary>
        /// <param name="messageSummary">Source <see cref="IMailFolder"/> and <see cref="UniqueId"/>.</param>
        /// <param name="destination">Destination mail folder.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns><see cref="UniqueId"/> of the moved message.</returns>
        Task<UniqueId?> MoveToAsync(IMessageSummary messageSummary, IMailFolder destination, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously move the specified message to the specified folder.
        /// </summary>
        /// <param name="messageSummary">Source <see cref="IMailFolder"/> and <see cref="UniqueId"/>.</param>
        /// <param name="destinationFolder">Name of the destination mail folder.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns><see cref="UniqueId"/> of the moved message.</returns>
        Task<UniqueId?> MoveToAsync(IMessageSummary messageSummary, string destinationFolder, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously move the specified message to the specified folder.
        /// </summary>
        /// <param name="messageSummary">Source <see cref="IMailFolder"/> and <see cref="UniqueId"/>.</param>
        /// <param name="mailFolder">Destination <see cref="IMailFolder"/></param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns><see cref="UniqueId"/> of the moved message.</returns>
        Task<UniqueId?> MoveToAsync(IMessageSummary messageSummary, SpecialFolder mailFolder = SpecialFolder.Sent, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously move the specified messages to the destination folder.
        /// </summary>
        /// <param name="uniqueIds"><see cref="UniqueId"/>s to move.</param>
        /// <param name="destination">Destination mail folder.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns><see cref="UniqueIdMap"/> of the messages moved to the <see cref="IMailFolder"/> destination.</returns>
        Task<UniqueIdMap> MoveToAsync(IEnumerable<UniqueId> uniqueIds, IMailFolder destination, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously move the specified messages to the destination folder.
        /// </summary>
        /// <param name="uniqueIds"><see cref="UniqueId"/>s to move.</param>
        /// <param name="destinationFolder">Name of the destination mail folder.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns><see cref="UniqueIdMap"/> of the messages moved to the <see cref="IMailFolder"/> destination.</returns>
        Task<UniqueIdMap> MoveToAsync(IEnumerable<UniqueId> uniqueIds, string destinationFolder, CancellationToken cancellationToken = default);

        /// <summary>
        /// Mail folder summary.
        /// </summary>
        /// <returns>Mail folder name and count.</returns>
        string ToString();
    }
}
