using System;
using System.Threading;
using System.Threading.Tasks;

namespace MailKitSimplified.Core.Abstractions
{
    public interface IEmailSender : IDisposable
    {
        IEmailWriter Email { get; }
        Task SendAsync(IEmail email, CancellationToken cancellationToken = default);
    }
}
