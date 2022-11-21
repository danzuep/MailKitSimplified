using MailKit;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MailKitSimplified.Receiver.Abstractions
{
    public interface IMailFolderClient : IAsyncDisposable, IDisposable
    {
        ValueTask<IMailFolder> ConnectAsync(bool enableWrite = false, CancellationToken cancellationToken = default);

        string ToString();
    }
}
