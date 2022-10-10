using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using MimeKit;
using MailKitSimplified.Core.Abstractions;

namespace MailKitSimplified.Sender.Abstractions
{
    public interface IMimeMessageSender : IEmailSender, IDisposable
    {
        Task SendAsync(MimeMessage mimeMessage, CancellationToken cancellationToken = default);
        Task SendAsync(MimeMessage mimeMessage, IEnumerable<string> attachmentFilePaths, CancellationToken cancellationToken = default);
        Task<bool> TrySendAsync(MimeMessage mimeMessage, IList<string> attachmentFilePaths, CancellationToken cancellationToken);
    }
}
