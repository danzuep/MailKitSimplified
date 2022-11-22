using MailKit;
using MailKit.Net.Imap;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace MailKitSimplified.Receiver.Abstractions
{
    public interface IImapReceiver : IAsyncDisposable, IDisposable
    {
        /// <summary>
        /// Read emails fluently from the default mail folder.
        /// </summary>
        IMailFolderReader ReadMail { get; }

        /// <summary>
        /// Connect to a mail folder and read emails fluently.
        /// </summary>
        /// <param name="mailFolderName">Mail folder to read from.</param>
        /// <returns><see cref="IMailReader"/>.</returns>
        IMailFolderReader ReadFrom(string mailFolderName);

        /// <summary>
        /// Get a list of the names of all the folders connected to this account.
        /// </summary>
        /// <param name="cancellationToken">Request cancellation token.</param>
        /// <returns>Names of all connected mail folders.</returns>
        ValueTask<IList<string>> GetMailFolderNamesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Connect and authenticate the IMAP client.
        /// </summary>
        /// <param name="cancellationToken">Stop connecting the client.</param>
        /// <returns>Connected <see cref="IImapClient">IMAP client</see>.</returns>
        ValueTask<IImapClient> ConnectImapClientAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Connect to the given mail folder.
        /// </summary>
        /// <param name="mailFolderName">Mail folder name.</param>
        /// <param name="cancellationToken">Request cancellation token.</param>
        /// <returns>Connected <see cref="IMailFolder"/>.</returns>
        ValueTask<IMailFolder> ConnectMailFolderAsync(string mailFolderName = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Connect an <see cref="IMailFolderClient"/> to the mail folder.
        /// </summary>
        /// <param name="mailFolderName">Mail folder name.</param>
        /// <param name="cancellationToken">Request cancellation token.</param>
        /// <returns><see cref="IMailFolderClient"/>.</returns>
        ValueTask<IMailFolderClient> ConnectMailFolderClientAsync(string mailFolderName = null, CancellationToken cancellationToken = default);
    }
}
