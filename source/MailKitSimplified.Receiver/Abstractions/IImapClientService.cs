using MailKit;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MailKitSimplified.Receiver.Abstractions
{
    public interface IImapClientService : IDisposable
    {
        ValueTask<IMailFolder> ConnectAsync(CancellationToken cancellationToken = default);
        ValueTask<IMailFolder> GetFolderAsync(string mailFolderName, CancellationToken ct = default);
    }
}
