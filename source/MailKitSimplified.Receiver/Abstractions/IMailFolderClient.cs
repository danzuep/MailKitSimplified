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
        /// <summary>
        /// Connect to the configured mail folder.
        /// </summary>
        /// <param name="enableWrite">Optionally enable ReadWrite access.</param>
        /// <param name="cancellationToken">Mail folder connection cancellation token.</param>
        /// <returns>Connected <see cref="IMailFolder"/>.</returns>
        ValueTask<IMailFolder> ConnectAsync(bool enableWrite = false, CancellationToken cancellationToken = default);

        /// <summary>
        /// Query the server for the unique IDs of messages with properties that match the search filters.
        /// </summary>
        /// <param name="searchQuery">Mail folder search query.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The first 250 <see cref="UniqueId"/>s.</returns>
        Task<IList<UniqueId>> SearchAsync(SearchQuery searchQuery, CancellationToken cancellationToken = default);

        /// <summary>
        /// Mail folder summary.
        /// </summary>
        /// <returns>Mail folder name and count.</returns>
        string ToString();
    }
}
