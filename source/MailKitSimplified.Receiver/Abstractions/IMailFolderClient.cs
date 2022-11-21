using MailKit;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MailKitSimplified.Receiver.Abstractions
{
    public interface IMailFolderClient : IAsyncDisposable, IDisposable
    {
        /// <summary>Connect to the configured mail folder.</summary>
        /// <param name="enableWrite">Optionally enable ReadWrite access.</param>
        /// <param name="cancellationToken">Mail folder connection cancellation token.</param>
        /// <returns>Connected <see cref="IMailFolder"/>.</returns>
        ValueTask<IMailFolder> ConnectAsync(bool enableWrite = false, CancellationToken cancellationToken = default);

        /// <summary>Mail folder summary.</summary>
        /// <returns>Mail folder name and count.</returns>
        string ToString();
    }
}
