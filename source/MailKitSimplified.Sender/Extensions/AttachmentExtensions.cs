using MimeKit;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace MailKitSimplified.Sender.Extensions
{
    [ExcludeFromCodeCoverage]
    public static class AttachmentExtensions
    {
        public static string ToEnumeratedString<T>(
            this IEnumerable<T> data, string div = ", ")
            => data is null ? "" : string.Join(div,
                data.Select(o => o?.ToString() ?? ""));

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
    }
}
