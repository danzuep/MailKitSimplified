using System;
using System.Threading;
using System.Threading.Tasks;
using MimeKit;

namespace MailKitSimplified.Sender.Abstractions
{
    public interface ISmtpSender : IDisposable
    {
        IEmailWriter WriteEmail { get; }

        Task SendAsync(MimeMessage mimeMessage, CancellationToken cancellationToken = default);

        Task<bool> TrySendAsync(MimeMessage mimeMessage, CancellationToken cancellationToken);
    }
}
