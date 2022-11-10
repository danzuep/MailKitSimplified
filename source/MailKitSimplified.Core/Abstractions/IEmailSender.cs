using System.Threading;
using System.Threading.Tasks;

namespace MailKitSimplified.Core.Abstractions
{
    public interface IEmailSender
    {
        IEmailWriter WriteEmail { get; }
        Task SendAsync(ISendableEmail email, CancellationToken cancellationToken = default);
        Task<bool> TrySendAsync(ISendableEmail email, CancellationToken cancellationToken = default);
    }
}
