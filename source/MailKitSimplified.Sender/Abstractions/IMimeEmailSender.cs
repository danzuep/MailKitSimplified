using MimeKit;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MailKitSimplified.Sender.Abstractions
{
    public interface IMimeEmailSender : IDisposable
    {
        IEmail Email { get; }
        Task SendAsync(MimeMessage mimeMessage, CancellationToken cancellationToken = default);
    }
}
