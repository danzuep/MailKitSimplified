using MailKit;
using MailKit.Net.Imap;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MailKitSimplified.Receiver.Abstractions
{
    public interface IImapReceiver : IDisposable
    {
        IMailReader ReadMail { get; }
        IMailReader ReadFrom(string mailFolderName);

        ValueTask<IImapClient> ConnectImapClientAsync(CancellationToken cancellationToken = default);

        ValueTask<IMailFolder> ConnectAsync(CancellationToken cancellationToken = default);
        ValueTask<IMailFolder> GetFolderAsync(string mailFolderName, CancellationToken cancellationToken = default);
    }
}
