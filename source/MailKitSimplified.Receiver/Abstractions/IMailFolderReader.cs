using MailKit;
using MimeKit;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace MailKitSimplified.Receiver.Abstractions
{
    public interface IMailFolderReader : IMailReader, IAsyncDisposable, IDisposable
    {
        /// <summary>
        /// Get message summaries with just the requested items filled in for the specified unique IDs.
        /// </summary>
        /// <param name="uniqueIds">Messages to download by <see cref="UniqueId">ID</see>.</param>
        /// <param name="filter"><see cref="MessageSummaryItems"/> to download.</param>
        /// <param name="cancellationToken">Request cancellation token.</param>
        /// <returns>Collection of <see cref="IMessageSummary"/> items.</returns>
        [Obsolete("Use Range(UidStart, UidEnd).GetMessageSummariesAsync() instead.")]
        Task<IList<IMessageSummary>> GetMessageSummariesAsync(IEnumerable<UniqueId> uniqueIds, MessageSummaryItems filter = MessageSummaryItems.UniqueId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get a <see cref="MimeMessage"/> by unique ID.
        /// </summary>
        /// <param name="uniqueId">Message to download by <see cref="UniqueId">ID</see>.</param>
        /// <param name="cancellationToken">Request cancellation token.</param>
        /// <param name="progress">The progress reporting mechanism.</param>
        /// <returns>Downloaded <see cref="MimeMessage"/>.</returns>
        [Obsolete("Use Range(uniqueId).GetMimeMessagesAsync() instead.")]
        Task<MimeMessage> GetMimeMessageAsync(UniqueId uniqueId, CancellationToken cancellationToken = default, ITransferProgress progress = null);

        /// <summary>
        /// Get <see cref="MimeMessage"/>s by their unique IDs.
        /// </summary>
        /// <param name="uniqueIds">Messages to download by <see cref="UniqueId">ID</see>.</param>
        /// <param name="cancellationToken">Request cancellation token.</param>
        /// <param name="progress">The progress reporting mechanism.</param>
        /// <returns>List of <see cref="MimeMessage"/> items.</returns>
        [Obsolete("Use Range(UidStart, UidEnd).GetMimeMessagesAsync() instead.")]
        Task<IList<MimeMessage>> GetMimeMessagesAsync(IEnumerable<UniqueId> uniqueIds, CancellationToken cancellationToken = default, ITransferProgress progress = null);

        /// <summary>
        /// Get a <see cref="MimeMessage"/> from an <see cref="IMessageSummary"/>.
        /// </summary>
        /// <param name="messageSummary">IMessageSummary to convert to a MimeMessage.</param>
        /// <param name="cancellationToken">Request cancellation token.</param>
        /// <returns>List of <see cref="MimeMessage"/> items.</returns>
        [Obsolete("Use messageSummary.GetMimeMessageEnvelopeBodyAsync() instead.")]
        Task<MimeMessage> GetMimeMessageEnvelopeBodyAsync(IMessageSummary messageSummary, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get <see cref="MimeMessage"/>s by their unique IDs.
        /// </summary>
        /// <param name="uniqueIds">Messages to download by <see cref="UniqueId">ID</see>.</param>
        /// <param name="cancellationToken">Request cancellation token.</param>
        /// <returns>List of <see cref="MimeMessage"/> items.</returns>
        [Obsolete("Use Range(UidStart, UidEnd).GetMimeMessagesEnvelopeBodyAsync() instead.")]
        Task<IList<MimeMessage>> GetMimeMessagesEnvelopeBodyAsync(IEnumerable<UniqueId> uniqueIds, CancellationToken cancellationToken = default);
    }
}
