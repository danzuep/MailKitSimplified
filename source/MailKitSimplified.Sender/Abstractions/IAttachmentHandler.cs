using MimeKit;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace MailKitSimplified.Sender.Abstractions
{
    public interface IAttachmentHandler
    {
        IList<MimePart> GetMimeParts(params string[] filePaths);
        Task<Stream> GetFileStreamAsync(string filePath, CancellationToken cancellationToken = default);
        Task<IList<MimeEntity>> GetMimeEntitiesAsync(IEnumerable<string> filePaths, CancellationToken cancellationToken = default);
        Task<IEnumerable<MimeEntity>> LoadFilePathAsync(string filePath, CancellationToken cancellationToken = default);
        Task<IEnumerable<MimeEntity>> LoadFilePathsAsync(IEnumerable<string> filePaths, CancellationToken cancellationToken = default);
        Task<MimeMessage> AddAttachmentsAsync(MimeMessage mimeMessage, IEnumerable<string> filePaths, CancellationToken cancellationToken = default);
    }
}
