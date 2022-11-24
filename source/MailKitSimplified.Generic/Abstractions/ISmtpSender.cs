using System;
using System.Threading;
using System.Threading.Tasks;

namespace MailKitSimplified.Core.Abstractions
{
    public interface ISmtpSender : IDisposable
    {
        ISendableEmailWriter WriteEmail { get; }

        Task SendAsync(ISendableEmail email, CancellationToken cancellationToken = default);
    }
}
