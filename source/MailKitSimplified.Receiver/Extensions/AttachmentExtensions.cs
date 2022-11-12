using MimeKit;
using MailKit;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace MailKitSimplified.Receiver.Extensions
{
    public static class AttachmentExtensions
    {
        public static IEnumerable<string> GetAttachmentNames(this IEnumerable<MimeEntity> mimeEntities)
        {
            return mimeEntities?.Select(a => a.GetAttachmentName()) ?? Array.Empty<string>();
        }

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

        public static IEnumerable<BodyPartBasic> GetMailAttachmentDetails(this IMessageSummary message, IList<string> suffix, bool getAllNonText = false)
        {
            IEnumerable<BodyPartBasic> attachments = Array.Empty<BodyPartBasic>();
            if (message?.Body is BodyPartMultipart multipart)
            {
                var attachmentParts = multipart.BodyParts.OfType<BodyPartBasic>()
                    .Where(p => (getAllNonText && !p.ContentType.MediaType.ToLowerInvariant().Contains("text")) || p.IsAttachment);
                attachments = suffix?.Count > 0 ? attachmentParts.Where(a => suffix.Any(s => a.FileName?
                    .EndsWith(s, StringComparison.OrdinalIgnoreCase) ?? false)) : attachmentParts;
            }
            return attachments.ToList();
        }

        public static IEnumerable<MimeEntity> GetFilteredAttachments(this IEnumerable<MimeEntity> mimeEntities, IEnumerable<string> mediaTypes)
        {
            return mediaTypes == null || mimeEntities == null ? Array.Empty<MimeEntity>() :
                mimeEntities.Where(a => a.IsAttachment && a is MimePart att && mediaTypes.Any(s =>
                    att.FileName?.EndsWith(s, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        public static async Task<MemoryStream> GetMimeEntityStream(this MimeEntity mimeEntity, CancellationToken ct = default)
        {
            var memoryStream = new MemoryStream();
            if (mimeEntity != null)
                await mimeEntity.WriteToStreamAsync(memoryStream, ct);
            memoryStream.Position = 0;
            return memoryStream;
        }

        public static async Task<Stream> WriteToStreamAsync(this MimeEntity entity, Stream stream, CancellationToken ct = default)
        {
            if (entity is MessagePart messagePart)
            {
                await messagePart.Message.WriteToAsync(stream, ct);
            }
            else if (entity is MimePart mimePart && mimePart.Content != null)
            {
                await mimePart.Content.DecodeToAsync(stream, ct);
            }
            // rewind the stream so the next process can read it from the beginning
            stream.Position = 0;
            return stream;
        }
    }
}
