using MailKit;
using MailKit.Net.Imap;
using MailKit.Security;
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
        /// Monitor the default mail folder with an email idle client.
        /// </summary>
        IMailFolderMonitor MonitorFolder { get; }

        /// <summary>
        /// Get disposable access to an <see cref="IMailFolder"/>.
        /// </summary>
        IMailFolderClient MailFolderClient { get; }

        /// <summary>
        /// Get the underlying <see cref="IImapClient"/>.
        /// </summary>
        IImapClient ImapClient { get; }

        /// <summary>
        /// Connect and authenticate the IMAP client.
        /// </summary>
        /// <param name="cancellationToken">Stop connecting the client.</param>
        /// <returns>Connected <see cref="IImapClient">IMAP client</see>.</returns>
        /// <exception cref="ImapProtocolException">Connection failed</exception>
        /// <exception cref="AuthenticationException">Failed to authenticate</exception>
        ValueTask<IImapClient> ConnectAuthenticatedImapClientAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Get a list of the names of all the folders connected to this account.
        /// </summary>
        /// <param name="cancellationToken">Request cancellation token.</param>
        /// <returns>Names of all connected mail folders.</returns>
        Task<IList<string>> GetMailFolderNamesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Connect to the given mail folder.
        /// </summary>
        /// <param name="cancellationToken">Request cancellation token.</param>
        /// <returns>Connected <see cref="IMailFolder"/>.</returns>
        ValueTask<IMailFolder> ConnectMailFolderAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Disconnect from the internal <see cref="IImapClient"/>.
        /// Note: GetAwaiter().GetResult() requires this to be a Task.
        /// </summary>
        /// <param name="cancellationToken">Request cancellation token.</param>
        /// <returns>Disconnected IMAP client.</returns>
        Task DisconnectAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Create a new IMAP receiver with the same settings and logger.
        /// The IMAP client is not re-entrant so a shallow copy would generate
        /// an <see cref="InvalidOperationException"/>.
        /// </summary>
        /// <returns>Configured IMAP receiver.</returns>
        IImapReceiver Clone();
    }
}
