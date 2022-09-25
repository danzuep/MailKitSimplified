using MimeKit;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MailKitSimplified.Sender.Abstractions
{
    public interface IMimeEmailSender : IDisposable
    {
        IFluentEmail Email { get; }
        Task SendAsync(MimeMessage mimeMessage, CancellationToken cancellationToken = default);
    }
}
