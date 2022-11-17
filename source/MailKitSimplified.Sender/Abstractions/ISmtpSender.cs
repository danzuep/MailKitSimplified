using MimeKit;
using MailKit;
using MailKit.Net.Smtp;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MailKitSimplified.Sender.Abstractions
{
    public interface ISmtpSender : IDisposable
    {
        IEmailWriter WriteEmail { get; }

        ValueTask<ISmtpClient> ConnectSmtpClientAsync(CancellationToken cancellationToken = default);

        Task SendAsync(MimeMessage mimeMessage, CancellationToken cancellationToken = default, ITransferProgress transferProgress = null);

        Task<bool> TrySendAsync(MimeMessage mimeMessage, CancellationToken cancellationToken = default, ITransferProgress transferProgress = null);
    }
}
