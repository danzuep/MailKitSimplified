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
        IMailReader ReadMail { get; }

        IMailReader ReadFrom(string mailFolderName);

        ValueTask<IList<string>> GetMailFolderNamesAsync(CancellationToken cancellationToken = default);

        ValueTask<IImapClient> ConnectImapClientAsync(CancellationToken cancellationToken = default);

        ValueTask<IMailFolder> ConnectMailFolderAsync(string mailFolderName = null, CancellationToken cancellationToken = default);

        ValueTask<IMailFolderClient> ConnectMailFolderClientAsync(string mailFolderName = null, bool enableWrite = false, CancellationToken cancellationToken = default);
    }
}
