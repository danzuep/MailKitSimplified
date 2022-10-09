using System;
using System.Threading;
using System.Threading.Tasks;

namespace MailKitSimplified.Core.Abstractions
{
    public interface IEmailSender
    {
        Task SendAsync(IEmail email, CancellationToken cancellationToken = default);
        Task<bool> TrySendAsync(IEmail email, CancellationToken cancellationToken = default);
    }
}
