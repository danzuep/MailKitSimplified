using MailKit;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace MailKitSimplified.Receiver.Abstractions
{
    public interface IMessageSummaryReader : IDisposable
    {
        //IEmailReceiver Create(NetworkCredential networkCredential);
        //IEmailReceiver Imap(string host, int port = 0);

        //IEmailReceiver Credential(NetworkCredential networkCredential);

        //Task ConnectImapClientAsync(CancellationToken cancellationToken = default);
        //Task<IMailFolder> OpenFolderAsync(string folderName, bool openFolder, bool enableWrite, CancellationToken cancellationToken = default);
        ////IEmail GetEmail { get; }
    }
}
