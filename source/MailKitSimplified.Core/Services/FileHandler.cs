using System.IO;
using System.IO.Abstractions;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MailKitSimplified.Core.Abstractions;

namespace MailKitSimplified.Core.Services
{
    [ExcludeFromCodeCoverage]
    public sealed class FileHandler : IFileHandler
    {
        private readonly ILogger _logger;
        private readonly IFileSystem _fileSystem;

        public FileHandler(ILogger<FileHandler> logger = null, IFileSystem fileSystem = null)
        {
            _logger = logger ?? NullLogger<FileHandler>.Instance;
            _fileSystem = fileSystem ?? new FileSystem();
        }

        /// <summary> 
        /// Ensures that the last character on the extraction
        /// path is the directory separator "\\" char.
        /// </summary>
        /// <param name="filePath">Path to check</param>
        /// <returns>Modified path</returns>
        public string NormaliseFilePath(string filePath)
        {
            if (!_fileSystem.Path.HasExtension(filePath) && !filePath.EndsWith(_fileSystem.Path.DirectorySeparatorChar.ToString()))
                filePath = $"{filePath}{_fileSystem.Path.DirectorySeparatorChar}";
            return filePath;
        }

        public bool FileCheckOk(string filePath, bool checkFile = false)
        {
            if (!checkFile)
                filePath = NormaliseFilePath(filePath);

            var directory = _fileSystem.Path.GetDirectoryName(filePath);
            bool isLocalDirectory = string.IsNullOrWhiteSpace(directory);
            bool directoryExists = isLocalDirectory || _fileSystem.Directory.Exists(directory);
            bool fileExists = directoryExists && checkFile && _fileSystem.File.Exists(filePath);

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
                using (var source = _fileSystem.File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
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
