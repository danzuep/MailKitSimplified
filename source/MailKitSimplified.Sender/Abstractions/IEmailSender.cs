using System;
using System.Threading;
using System.Threading.Tasks;

namespace MailKitSimplified.Sender.Abstractions
{
    public interface IEmailSender : IDisposable
    {
        IFluentEmail Email { get; }
        Task SendAsync(IEmail email, CancellationToken cancellationToken = default);
    }
}
