using MimeKit;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace MailKitSimplified.Receiver.Extensions
{
    [ExcludeFromCodeCoverage]
    public static class AttachmentExtensions
    {
        public static string GetAttachmentName(this MimeEntity mimeEntity)
        {
            string fileName = string.Empty;
            if (mimeEntity is MimePart mimePart)
                fileName = mimePart.FileName;
            else if (mimeEntity is MessagePart msgPart)
                fileName = msgPart.Message?.MessageId ??
                    msgPart.Message?.References?.FirstOrDefault() ??
                    msgPart.GetHashCode() + ".eml";
            else if (mimeEntity is MimeKit.Tnef.TnefPart tnefPart)
                fileName = tnefPart.ExtractAttachments()
                    .Select(t => t?.ContentDisposition?.FileName)
                    .ToEnumeratedString(".tnef, ");
            else
                fileName = mimeEntity?.ContentDisposition?.FileName ?? "";
            return fileName ?? "";
        }

        public static IEnumerable<string> GetAttachmentNames(this IEnumerable<MimeEntity> mimeEntities) =>
            mimeEntities?.Select(a => a.GetAttachmentName()) ?? Array.Empty<string>();

        public static IEnumerable<MimeEntity> GetFilteredAttachments(this IEnumerable<MimeEntity> mimeEntities, IEnumerable<string> fileTypeSuffix) =>
            fileTypeSuffix == null || mimeEntities == null ? Array.Empty<MimeEntity>() :
                mimeEntities.Where(a => a.IsAttachment && a is MimePart att && fileTypeSuffix.Any(s =>
                    att.FileName?.EndsWith(s, StringComparison.OrdinalIgnoreCase) ?? false));

        public static async Task<MemoryStream> GetMimeEntityStream(this MimeEntity mimeEntity, CancellationToken cancellationToken = default)
        {
            var memoryStream = new MemoryStream();
            if (mimeEntity != null)
                await mimeEntity.WriteToStreamAsync(memoryStream, cancellationToken);
            memoryStream.Position = 0;
            return memoryStream;
        }

        public static async Task<Stream> WriteToStreamAsync(this MimeEntity entity, Stream stream, CancellationToken cancellationToken = default)
        {
            if (entity != null && stream != null)
            {
                if (entity is MessagePart messagePart)
                {
                    await messagePart.Message.WriteToAsync(stream, cancellationToken);
                }
                else if (entity is MimePart mimePart && mimePart.Content != null)
                {
                    await mimePart.Content.DecodeToAsync(stream, cancellationToken);
                }
                // rewind the stream so the next process can read it from the beginning
                stream.Position = 0;
            }
            return stream;
        }

        public static async Task<MemoryStream> WriteToStreamAsync(this MimeEntity mimeEntity, CancellationToken cancellationToken = default)
        {
            var memoryStream = new MemoryStream();
            if (mimeEntity != null)
                await mimeEntity.WriteToAsync(memoryStream, cancellationToken);
            memoryStream.Position = 0;
            return memoryStream;
        }

        public static async Task<MemoryStream> WriteToStreamAsync(this MimeMessage mimeMessage, CancellationToken cancellationToken = default)
        {
            var memoryStream = new MemoryStream();
            if (mimeMessage != null)
                await mimeMessage.WriteToAsync(memoryStream, cancellationToken);
            memoryStream.Position = 0;
            return memoryStream;
        }
    }
}
