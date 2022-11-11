using MimeKit;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MailKitSimplified.Sender.Abstractions
{
    public interface IAttachmentHandler
    {
        Task<Stream> GetFileStreamAsync(string filePath, CancellationToken cancellationToken = default);
        Task<IEnumerable<MimePart>> LoadFilePathAsync(string filePath, CancellationToken cancellationToken = default);
        Task<IEnumerable<MimePart>> LoadFilePathsAsync(IEnumerable<string> filePaths, CancellationToken cancellationToken = default);
        Task<MimeMessage> AddAttachmentsAsync(MimeMessage mimeMessage, IEnumerable<string> filePaths, CancellationToken cancellationToken = default);
    }
}
