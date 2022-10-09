using MimeKit;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MailKitSimplified.Sender.Abstractions
{
    public interface IMimeAttachmentHandler
    {
        Task<IEnumerable<MimeEntity>> LoadFilePathAsync(string filePath, CancellationToken cancellationToken = default);
        Task<IEnumerable<MimeEntity>> LoadFilePathsAsync(IEnumerable<string> filePaths, CancellationToken cancellationToken = default);
        Task<MimeMessage> AddAttachments(MimeMessage mimeMessage, IEnumerable<string> filePaths, CancellationToken cancellationToken = default);
    }
}
