using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace MailKitSimplified.Core.Abstractions
{
    public interface IEmail : IEmailHeader
    {
        IList<string> AttachmentFilePaths { get; set; }
        string Subject { get; set; }
        string Body { get; set; }
        bool IsHtml { get; set; }

        Task SendAsync(CancellationToken token = default);
        Task<bool> TrySendAsync(CancellationToken cancellationToken = default);
    }
}
