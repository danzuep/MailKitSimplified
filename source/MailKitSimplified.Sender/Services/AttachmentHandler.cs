using System;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using System.IO.Abstractions;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging;
using MimeKit;
using MimeKit.Utils;
using MailKitSimplified.Sender.Abstractions;

namespace MailKitSimplified.Sender.Services
{
    [ExcludeFromCodeCoverage]
    public sealed class AttachmentHandler : IAttachmentHandler
    {
        private readonly ILogger _logger;
        private readonly IFileSystem _fileSystem;

        public AttachmentHandler(ILogger<AttachmentHandler> logger = null, IFileSystem fileSystem = null)
        {
            _logger = logger ?? NullLogger<AttachmentHandler>.Instance;
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
            if (!_fileSystem.Path.HasExtension(filePath) && !filePath.EndsWith($"{_fileSystem.Path.DirectorySeparatorChar}"))
                filePath = $"{filePath}{_fileSystem.Path.DirectorySeparatorChar}";
            return filePath;
        }

        public bool CheckFileExists(string filePath, bool checkFile = true)
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
            if (CheckFileExists(filePath, true))
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

        public static MimePart GetMimePart(Stream stream, string fileName, string contentType = "", string contentId = "")
        {
            MimePart mimePart = null;
            if (stream != null && stream.Length > 0)
            {
                stream.Position = 0; // reset stream position ready to read
                if (string.IsNullOrWhiteSpace(contentType))
                    contentType = MimeTypes.GetMimeType(fileName);
                if (string.IsNullOrWhiteSpace(contentId))
                    contentId = MimeUtils.GenerateMessageId();
                var attachment = MimeKit.ContentDisposition.Attachment;
                mimePart = new MimePart(contentType)
                {
                    Content = new MimeContent(stream),
                    ContentTransferEncoding = ContentEncoding.Base64,
                    ContentDisposition = new MimeKit.ContentDisposition(attachment),
                    ContentId = contentId,
                    FileName = fileName
                };
            }
            return mimePart;
        }

        public IList<MimePart> GetMimeParts(params string[] filePaths)
        {
            var mimeEntities = new List<MimePart>();
            if (filePaths != null && filePaths.Length > 0)
            {
                foreach (var filePath in filePaths)
                {
                    if (!string.IsNullOrWhiteSpace(filePath) && _fileSystem.File.Exists(filePath))
                    {
                        using (var stream = _fileSystem.File.OpenRead(filePath))
                        {
                            string fileName = _fileSystem.Path.GetFileName(filePath);
                            var mimeEntity = GetMimePart(stream, fileName);
                            mimeEntities.Add(mimeEntity);
                        }
                    }
                }
            }
            return mimeEntities;
        }

        public async Task<IList<MimeEntity>> GetMimeEntitiesAsync(IEnumerable<string> filePaths, CancellationToken cancellationToken = default)
        {
            var mimeEntities = new List<MimeEntity>();
            if (filePaths != null)
            {
                foreach (var filePath in filePaths)
                {
                    var mimeEntity = await GetMimeEntityAsync(filePath, cancellationToken).ConfigureAwait(false);
                    if (mimeEntity != null)
                        mimeEntities.Add(mimeEntity);
                }
            }
            return mimeEntities;
        }

        public async Task<MimeEntity> GetMimeEntityAsync(string filePath, CancellationToken cancellationToken = default)
        {
            MimeEntity mimeEntity = null;
            if (!string.IsNullOrWhiteSpace(filePath) && _fileSystem.File.Exists(filePath))
            {
                using (var stream = await GetFileStreamAsync(filePath, cancellationToken).ConfigureAwait(false))
                {
                    if (stream != null)
                    {
                        string fileName = _fileSystem.Path.GetFileName(filePath);
                        mimeEntity = GetMimePart(stream, fileName);
                        // mimeEntity = new BodyBuilder().Attachments.Add(filePath);
                        // The following is to ensure the same behaviour as above:
                        if (mimeEntity.ContentType == new MimeKit.ContentType("message", "rfc822"))
                        {
                            var mimeMessage = await MimeMessage.LoadAsync(stream, cancellationToken);
                            mimeEntity = new MessagePart { Message = mimeMessage };
                        }
                    }
                }
            }
            return mimeEntity;
        }

        public async Task<IEnumerable<MimeEntity>> LoadFilePathAsync(string filePath, CancellationToken cancellationToken = default)
        {
            var separator = new char[] { '|' }; //';' is a valid attachment file name character
            var filePaths = filePath?.Split(separator, StringSplitOptions.RemoveEmptyEntries);
            var results = await LoadFilePathsAsync(filePaths, cancellationToken).ConfigureAwait(false);
            return results;
        }

        public async Task<IEnumerable<MimeEntity>> LoadFilePathsAsync(IEnumerable<string> filePaths, CancellationToken cancellationToken = default)
        {
            IList<MimeEntity> results = Array.Empty<MimeEntity>();
            if (filePaths?.Any() ?? false)
            {
                var mimeEntityTasks = filePaths.Select(name => GetMimeEntityAsync(name, cancellationToken));
                var mimeEntities = await Task.WhenAll(mimeEntityTasks).ConfigureAwait(false);
                results = mimeEntities.Where(entity => entity != null).ToList();
                _logger.LogDebug($"{results.Count} attachments loaded.");
            }
            return results;
        }

        public async Task<MimeMessage> AddAttachmentsAsync(MimeMessage mimeMessage, IEnumerable<string> filePaths, CancellationToken cancellationToken = default)
        {
            var mimeParts = await LoadFilePathsAsync(filePaths, cancellationToken).ConfigureAwait(false);
            if (mimeMessage != null && mimeParts.Any())
            {
                var multipart = new Multipart();
                if (mimeMessage.Body != null)
                    multipart.Add(mimeMessage.Body);
                foreach (var mimePart in mimeParts)
                    multipart.Add(mimePart);
                mimeMessage.Body = multipart;
            }
            return mimeMessage;
        }
    }
}
