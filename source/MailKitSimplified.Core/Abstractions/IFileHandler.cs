using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MailKitSimplified.Core.Abstractions
{
    public interface IFileHandler
    {
        Task<Stream> GetFileStreamAsync(string filePath, CancellationToken cancellationToken = default);
    }
}
