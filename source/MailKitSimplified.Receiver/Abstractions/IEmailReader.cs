using MailKit;
using System.Threading;
using System.Threading.Tasks;

namespace MailKitSimplified.Receiver.Abstractions
{
    public interface IEmailReader
    {
        IEmailReader MailFolderName(string mailFolderName);

        IEmailReader Skip(int count);

        IEmailReader Take(int count);

        ValueTask<IMailFolder> ConnectAsync(CancellationToken cancellationToken = default);
    }
}
