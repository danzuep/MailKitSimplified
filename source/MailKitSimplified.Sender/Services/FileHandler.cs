using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MailKitSimplified.Sender.Abstractions;

namespace MailKitSimplified.Sender.Services
{
    //[ExcludeFromCodeCoverage]
    public sealed class FileHandler : IFileHandler
    {
        private readonly ILogger _logger;

        public FileHandler(ILogger<FileHandler> logger)
        {
            _logger = logger ?? NullLogger<FileHandler>.Instance;
        }

        /// <summary> 
        /// Ensures that the last character on the extraction
        /// path is the directory separator "\\" char.
        /// </summary>
        /// <param name="filePath">Path to check</param>
        /// <returns>Modified path</returns>
        public static string NormaliseFilePath(string filePath)
        {
            if (!Path.HasExtension(filePath) && !filePath.EndsWith(Path.DirectorySeparatorChar.ToString()))
                filePath = $"{filePath}{Path.DirectorySeparatorChar}";
            return filePath;
        }

        public bool FileCheckOk(string filePath, bool checkFile = false)
        {
            if (!checkFile)
                filePath = NormaliseFilePath(filePath);

            var directory = Path.GetDirectoryName(filePath);
            bool isLocalDirectory = string.IsNullOrWhiteSpace(directory);
            bool directoryExists = isLocalDirectory || Directory.Exists(directory);
            bool fileExists = directoryExists && checkFile && File.Exists(filePath);

            if (!directoryExists)
                _logger.LogWarning($"Folder not found: '{directory}'");
            else if (!fileExists)
                _logger.LogWarning($"File not found: '{filePath}'");

            return fileExists;
        }

        public async Task<Stream> GetFileStreamAsync(string filePath, CancellationToken cancellationToken = default)
        {
            const int BufferSize = 8192;
            var outputStream = new MemoryStream();
            if (FileCheckOk(filePath, true))
            {
                using (var source = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, useAsync: true))
                {
                    await source.CopyToAsync(outputStream, BufferSize, cancellationToken).ConfigureAwait(false);
                };
                outputStream.Position = 0;
                _logger.LogDebug($"Loaded to file-stream: '{filePath}'");
            }
            return outputStream;
        }
    }
}
