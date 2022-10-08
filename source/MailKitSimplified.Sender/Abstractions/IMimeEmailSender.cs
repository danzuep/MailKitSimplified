using MimeKit;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using MailKitSimplified.Core.Abstractions;

namespace MailKitSimplified.Sender.Abstractions
{
    public interface IMimeEmailSender : IEmailSender
    {
        Task SendAsync(MimeMessage mimeMessage, CancellationToken cancellationToken = default);
        Task SendAsync(MimeMessage mimeMessage, IEnumerable<string> attachmentFilePaths, CancellationToken cancellationToken = default);
    }
}
