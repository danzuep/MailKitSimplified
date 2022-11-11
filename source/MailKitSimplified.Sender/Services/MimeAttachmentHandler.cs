using System;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging;
using MimeKit;
using MimeKit.Utils;
using MailKitSimplified.Core.Abstractions;
using MailKitSimplified.Core.Services;
using MailKitSimplified.Sender.Abstractions;

namespace MailKitSimplified.Sender.Services
{
    public sealed class MimeAttachmentHandler : IMimeAttachmentHandler
    {
        private readonly ILogger _logger;
        private readonly IFileHandler _fileHandler;

        public MimeAttachmentHandler(ILogger<MimeAttachmentHandler> logger = null, IFileHandler fileHandler = null)
        {
            _logger = logger ?? NullLogger<MimeAttachmentHandler>.Instance;
            _fileHandler = fileHandler ?? new FileHandler(NullLogger<FileHandler>.Instance);
        }

        public static MimeEntity GetMimePart(Stream stream, string fileName, string contentType = "", string contentId = "")
        {
            MimeEntity result = null;
            if (stream != null && stream.Length > 0)
            {
                stream.Position = 0; // reset stream position ready to read
                if (string.IsNullOrWhiteSpace(contentType))
                    contentType = MediaTypeNames.Application.Octet;
                if (string.IsNullOrWhiteSpace(contentId))
                    contentId = MimeUtils.GenerateMessageId();
                var attachment = MimeKit.ContentDisposition.Attachment;
                result = new MimePart(contentType)
                {
                    Content = new MimeContent(stream),
                    ContentTransferEncoding = ContentEncoding.Base64,
                    ContentDisposition = new MimeKit.ContentDisposition(attachment),
                    ContentId = contentId,
                    FileName = fileName
                };
            }
            return result;
        }

        public async Task<MimeEntity> GetMimeEntityFromFilePathAsync(string filePath, string mediaType = MediaTypeNames.Application.Octet, CancellationToken cancellationToken = default)
        {
            MimeEntity result = null;
            if (!string.IsNullOrWhiteSpace(filePath))
            {
                var stream = await _fileHandler.GetFileStreamAsync(filePath, cancellationToken).ConfigureAwait(false);
                if (stream != null)
                {
                    string fileName = Path.GetFileName(filePath);
                    string contentType = Path.GetExtension(fileName)
                        .Equals(".pdf", StringComparison.OrdinalIgnoreCase) ?
                            MediaTypeNames.Application.Pdf : mediaType;
                    result = GetMimePart(stream, fileName, contentType);
                }
            }
            return result;
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
                var mimeEntityTasks = filePaths.Select(name => GetMimeEntityFromFilePathAsync(name, cancellationToken: cancellationToken));
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
