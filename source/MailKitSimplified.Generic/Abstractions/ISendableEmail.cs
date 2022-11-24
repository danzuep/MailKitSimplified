using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace MailKitSimplified.Core.Abstractions
{
    public interface ISendableEmail : IBasicEmail
    {
        IList<string> AttachmentFilePaths { get; set; }

        Task SendAsync(CancellationToken cancellationToken = default);

        Task<bool> TrySendAsync(CancellationToken cancellationToken = default);
    }
}
