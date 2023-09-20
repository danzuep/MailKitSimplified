using MimeKit;
using MailKit;
using MailKit.Search;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;

namespace MailKitSimplified.Receiver.Abstractions
{
    public interface IMailReader
    {
        /// <summary>
        /// Offset to start getting messages from.
        /// </summary>
        /// <param name="skipCount">Offset to start getting messages from.</param>
        /// <returns>Fluent <see cref="IMailReader"/>.</returns>
        IMailReader Skip(int skipCount);

        /// <summary>
        /// Number of messages to return.
        /// </summary>
        /// <param name="takeCount">Number of messages to return.</param>
        /// <param name="continuous">Whether to keep adding the offset or not.</param>
        /// <returns>Fluent <see cref="IMailReader"/>.</returns>
        IMailReader Take(int takeCount, bool continuous = false);

        /// <summary>
        /// UniqueId range of messages to get.
        /// </summary>
        /// <param name="start">First UniqueId to get.</param>
        /// <param name="end">Last UniqueId to get.</param>
        /// <param name="continuous">Default is to not continue in batches.</param>
        /// <returns>Fluent <see cref="IMailReader"/>.</returns>
        IMailReader Range(UniqueId start, UniqueId end, bool continuous = false);

        /// <summary>
        /// UniqueId range of messages to get.
        /// </summary>
        /// <param name="start">First UniqueId to get.</param>
        /// <param name="batchSize">Zero-indexed batch size.</param>
        /// <param name="continuous">Default is to continue in batches.</param>
        /// <returns>Fluent <see cref="IMailReader"/>.</returns>
        IMailReader Range(UniqueId start, ushort batchSize = 0, bool continuous = true);

        /// <summary>
        /// Set a query for searching messages in a <see cref="IMailFolder"/>.
        /// MailKit limits queries to 250 results by default, so this method overwrites Take to 250.
        /// MailKit returns results in ascending order by default, use Reverse() to get descending results.
        /// </summary>
        /// <param name="searchQuery">What to search for, e.g. SearchQuery.NotSeen.</param>
        /// <returns>Fluent <see cref="IMailReader"/>.</returns>
        IMailReader Query(SearchQuery searchQuery);

        /// <summary>
        /// Set which message summary parts to fetch. UniqueId is always included.
        /// </summary>
        /// <param name="messageSummaryItems">Message summary item filter.</param>
        /// <returns>Fluent <see cref="IMailReader"/>.</returns>
        IMailReader Items(MessageSummaryItems messageSummaryItems);

        /// <summary>
        /// Get a list of the message summaries with just the requested MessageSummaryItems.
        /// MailKit returns results in ascending order by default, use Reverse() to get descending results.
        /// </summary>
        /// <param name="filter"><see cref="MessageSummaryItems"/> to download.</param>
        /// <param name="cancellationToken">Request cancellation token.</param>
        /// <returns>List of <see cref="IMessageSummary"/> items.</returns>
        [Obsolete("Use Items().GetMessageSummariesAsync() instead.")]
        Task<IList<IMessageSummary>> GetMessageSummariesAsync(MessageSummaryItems filter, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get a list of the message summaries with basic MessageSummaryItems.
        /// MailKit returns results in ascending order by default, use Reverse() to get descending results.
        /// </summary>
        /// <param name="cancellationToken">Request cancellation token.</param>
        /// <returns>List of <see cref="IMessageSummary"/> items.</returns>
        Task<IList<IMessageSummary>> GetMessageSummariesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Get a list of <see cref="MimeMessage"/>s.
        /// MailKit returns results in ascending order by default, use Reverse() to get descending results.
        /// </summary>
        /// <param name="cancellationToken">Request cancellation token.</param>
        /// <param name="transferProgress">Current email download progress</param>
        /// <returns>List of all <see cref="MimeMessage"/> items.</returns>
        Task<IList<MimeMessage>> GetMimeMessagesAsync(CancellationToken cancellationToken = default, ITransferProgress transferProgress = null);

        /// <summary>
        /// Get the <see cref="Envelope"/> and Body of <see cref="MimeMessage"/>s.
        /// MailKit returns results in ascending order by default, use Reverse() to get descending results.
        /// </summary>
        /// <param name="cancellationToken">Request cancellation token.</param>
        /// <returns>List of <see cref="MimeMessage"/> items.</returns>
        Task<IList<MimeMessage>> GetMimeMessagesEnvelopeBodyAsync(CancellationToken cancellationToken = default);
    }
}
