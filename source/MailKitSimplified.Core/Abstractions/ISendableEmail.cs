using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace MailKitSimplified.Core.Abstractions
{
    public interface ISendableEmail : IEmailBase
    {
        IList<string> AttachmentFilePaths { get; set; }
        IEnumerable<string> AttachmentFileNames { get; }
        Task SendAsync(CancellationToken cancellationToken = default);
        Task<bool> TrySendAsync(CancellationToken cancellationToken = default);
    }
}
