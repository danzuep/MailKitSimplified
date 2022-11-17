using MailKit;
using MailKit.Net.Imap;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace MailKitSimplified.Receiver.Abstractions
{
    public interface IImapReceiver : IDisposable
    {
        IMailReader ReadMail { get; }

        IMailReader ReadFrom(string mailFolderName);

        ValueTask<IImapClient> ConnectImapClientAsync(CancellationToken cancellationToken = default);

        ValueTask<IList<string>> GetMailFolderNamesAsync(CancellationToken cancellationToken = default);

        ValueTask<IMailFolder> ConnectMailFolderAsync(string mailFolderName = null, CancellationToken cancellationToken = default);
    }
}
