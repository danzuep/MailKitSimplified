using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MailKitSimplified.Generic.Models
{
    /// <summary>
    /// Immutable attachment metadata with deferred, replayable content.
    /// </summary>
    public sealed class EmailAttachment
    {
        private readonly Func<CancellationToken, Task<Stream>> _openReadAsync;

        /// <summary>Gets the attachment's display file name.</summary>
        public string FileName { get; }

        /// <summary>Gets the MIME content type.</summary>
        public string ContentType { get; }

        /// <summary>Gets the optional content ID used by related HTML content.</summary>
        public string ContentId { get; }

        /// <summary>Gets whether the attachment is displayed inline.</summary>
        public bool IsInline { get; }

        /// <summary>
        /// Creates an attachment whose factory opens a fresh readable stream for each attempt.
        /// </summary>
        /// <param name="fileName">The display file name.</param>
        /// <param name="openReadAsync">A factory transferring ownership of a fresh stream to the consumer.</param>
        /// <param name="contentType">The MIME content type.</param>
        /// <param name="contentId">An optional content ID without header delimiters.</param>
        /// <param name="isInline">Whether the attachment is inline; requires a content ID.</param>
        public EmailAttachment(string fileName, Func<CancellationToken, Task<Stream>> openReadAsync,
            string contentType = "application/octet-stream", string contentId = null, bool isInline = false)
        {
            FileName = ValidateMetadata(fileName, nameof(fileName));
            ContentType = ValidateMetadata(contentType, nameof(contentType));
            ContentId = contentId == null ? null : ValidateMetadata(contentId, nameof(contentId));
            if (isInline && ContentId == null)
                throw new ArgumentException("Inline attachments require a content ID.", nameof(contentId));
            IsInline = isInline;
            _openReadAsync = openReadAsync ?? throw new ArgumentNullException(nameof(openReadAsync));
        }

        /// <summary>Creates an attachment by copying the supplied bytes.</summary>
        /// <param name="fileName">The display file name.</param>
        /// <param name="content">The content copied at construction time.</param>
        /// <param name="contentType">The MIME content type.</param>
        /// <param name="contentId">An optional content ID.</param>
        /// <param name="isInline">Whether the attachment is inline.</param>
        /// <returns>An attachment opening independent read-only streams.</returns>
        public static EmailAttachment FromBytes(string fileName, byte[] content,
            string contentType = "application/octet-stream", string contentId = null, bool isInline = false)
        {
            if (content == null)
                throw new ArgumentNullException(nameof(content));
            var snapshot = (byte[])content.Clone();
            return new EmailAttachment(fileName, cancellationToken => Task.FromResult<Stream>(
                new MemoryStream(snapshot, 0, snapshot.Length, false, false)), contentType, contentId, isInline);
        }

        /// <summary>Creates an attachment without opening or checking the file.</summary>
        /// <param name="filePath">The path pinned to an absolute path at construction time.</param>
        /// <param name="contentType">The MIME content type.</param>
        /// <returns>An attachment opening a new asynchronous file stream per attempt.</returns>
        /// <remarks>The file must remain available and unchanged for stable replay.</remarks>
        public static EmailAttachment FromFile(string filePath, string contentType = "application/octet-stream")
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("A file path is required.", nameof(filePath));
            var absolutePath = Path.GetFullPath(filePath);
            return new EmailAttachment(Path.GetFileName(absolutePath), cancellationToken => Task.FromResult<Stream>(
                new FileStream(absolutePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096,
                    FileOptions.Asynchronous | FileOptions.SequentialScan)), contentType);
        }

        /// <summary>Opens fresh content and transfers stream ownership to the caller.</summary>
        /// <param name="cancellationToken">Cancels materialization.</param>
        /// <returns>A readable stream that the caller must dispose.</returns>
        /// <remarks>Factories must not return a shared stream or retain ownership of a returned stream.</remarks>
        public async Task<Stream> OpenReadAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var stream = await _openReadAsync(cancellationToken).ConfigureAwait(false);
            if (stream == null)
                throw new InvalidOperationException("The attachment factory returned no stream.");
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!stream.CanRead)
                    throw new InvalidOperationException("The attachment factory returned an unreadable stream.");
                return stream;
            }
            catch
            {
                stream.Dispose();
                throw;
            }
        }

        private static string ValidateMetadata(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value) || value.IndexOfAny(new[] { '\r', '\n', '\0' }) >= 0)
                throw new ArgumentException("Attachment metadata must be nonempty and contain no header delimiters.", parameterName);
            return value;
        }
    }
}