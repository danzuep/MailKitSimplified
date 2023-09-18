using MimeKit;
using MimeKit.IO;
using MimeKit.Utils;
using System;
using System.Threading.Tasks;
using System.Threading;

namespace MailKitSimplified.Sender.Extensions
{
    public static class MimeMessageConverter
    {
        [Obsolete("Use MimeMessage.CopyAsync() instead")]
        internal static MimeMessage CopyAsTemplate(this MimeMessage original)
        {
            var copy = new MimeMessage();
            if (original.From.Count > 0)
                copy.From.AddRange(original.From);
            if (original.To.Count > 0)
                copy.To.AddRange(original.To);
            if (original.Cc.Count > 0)
                copy.Cc.AddRange(original.Cc);
            if (original.Bcc.Count > 0)
                copy.Bcc.AddRange(original.Bcc);
            if (original.Sender != null)
                copy.Sender = original.Sender;
            if (original.ReplyTo.Count > 0)
                copy.ReplyTo.AddRange(original.ReplyTo);
            if (!string.IsNullOrEmpty(original.MessageId))
                copy.MessageId = MimeUtils.GenerateMessageId();
            copy.Subject = original.Subject;
            if (original.Body != null)
                copy.Body = original.Body;
            return copy;
        }

        private static MimeMessage CloneStreamReferences(this MimeMessage mimeMessage, bool persistent, MemoryBlockStream memoryBlockStream = null, CancellationToken cancellationToken = default)
        {
            if (memoryBlockStream == null)
                memoryBlockStream = new MemoryBlockStream();
            mimeMessage.WriteTo(memoryBlockStream, cancellationToken);
            memoryBlockStream.Position = 0;
            var result = MimeMessage.Load(memoryBlockStream, persistent, cancellationToken);
            return result;
        }

        internal static MimeMessage Copy(this MimeMessage mimeMessage, CancellationToken cancellationToken = default) =>
            mimeMessage.CloneStreamReferences(true, null, cancellationToken);

        internal static MimeMessage Clone(this MimeMessage mimeMessage, CancellationToken cancellationToken = default) =>
            mimeMessage.CloneStreamReferences(false, null, cancellationToken);

        internal static async Task<MimeMessage> CloneStreamReferencesAsync(this MimeMessage mimeMessage, bool persistent, MemoryBlockStream memoryBlockStream = null, CancellationToken cancellationToken = default)
        {
            if (memoryBlockStream == null)
                memoryBlockStream = new MemoryBlockStream();
            await mimeMessage.WriteToAsync(memoryBlockStream, cancellationToken);
            memoryBlockStream.Position = 0;
            var result = await MimeMessage.LoadAsync(memoryBlockStream, persistent, cancellationToken).ConfigureAwait(false);
            if (persistent)
                result.MessageId = MimeUtils.GenerateMessageId();
            return result;
        }

        public static async Task<MimeMessage> CopyAsync(this MimeMessage mimeMessage, CancellationToken cancellationToken = default) =>
            await mimeMessage.CloneStreamReferencesAsync(true, null, cancellationToken).ConfigureAwait(false);

        public static async Task<MimeMessage> CloneAsync(this MimeMessage mimeMessage, CancellationToken cancellationToken = default) =>
            await mimeMessage.CloneStreamReferencesAsync(false, null, cancellationToken).ConfigureAwait(false);
    }
}
