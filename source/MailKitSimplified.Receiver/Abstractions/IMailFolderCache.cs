using System.Threading;
using System.Threading.Tasks;
using MailKit;
using MailKitSimplified.Receiver.Abstractions;

namespace MailKitSimplified.Receiver.Services
{
    public interface IMailFolderCache
    {
        Task<IMailFolder> GetMailFolderAsync(IImapReceiver imapReceiver, string mailFolderFullName, bool createIfMissing = false, CancellationToken cancellationToken = default);
        Task<UniqueId?> MoveToAsync(IImapReceiver imapReceiver, IMessageSummary messageSummary, string destinationFolderFullName, CancellationToken cancellationToken = default);
    }
}