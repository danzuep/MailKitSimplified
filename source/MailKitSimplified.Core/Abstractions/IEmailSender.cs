using System;
using System.Threading;
using System.Threading.Tasks;

namespace MailKitSimplified.Core.Abstractions
{
    public interface IEmailSender : IDisposable
    {
        IEmailWriter WriteEmail { get; }
        Task SendAsync(IEmail email, CancellationToken cancellationToken = default);
    }
}
