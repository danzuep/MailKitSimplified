using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using MailKitSimplified.Core.Models;

namespace MailKitSimplified.Core.Abstractions
{
    public interface IEmail
    {
        IList<EmailContact> From { get; set; }
        IList<EmailContact> To { get; set; }
        IList<string> AttachmentFilePaths { get; set; }
        string Subject { get; set; }
        string Body { get; set; }
        bool IsHtml { get; set; }

        Task SendAsync(CancellationToken token = default);
        Task<bool> TrySendAsync(CancellationToken cancellationToken = default);
    }
}
