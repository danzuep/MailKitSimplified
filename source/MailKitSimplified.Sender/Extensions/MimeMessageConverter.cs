using MimeKit;
using MimeKit.IO;
using MimeKit.Utils;
using System.Threading.Tasks;
using System.Threading;

namespace MailKitSimplified.Sender.Extensions
{
    public static class MimeMessageConverter
    {
        internal static MimeMessage CloneStreamReferences(this MimeMessage mimeMessage, bool persistent, MemoryBlockStream memoryBlockStream = null, CancellationToken cancellationToken = default)
        {
            if (memoryBlockStream == null)
                memoryBlockStream = new MemoryBlockStream();
            mimeMessage.WriteTo(memoryBlockStream, cancellationToken);
            memoryBlockStream.Position = 0;
            var result = MimeMessage.Load(memoryBlockStream, persistent, cancellationToken);
            //await memoryBlockStream.FlushAsync().ConfigureAwait(false);
            // MemoryStream doesn't have any unmanaged resources, so it doesn't need to be disposed.
            if (persistent)
                result.MessageId = MimeUtils.GenerateMessageId();
            return result;
        }

        public static MimeMessage Copy(this MimeMessage mimeMessage, CancellationToken cancellationToken = default) =>
            mimeMessage.CloneStreamReferences(true, null, cancellationToken);

        public static MimeMessage Clone(this MimeMessage mimeMessage, CancellationToken cancellationToken = default) =>
            mimeMessage.CloneStreamReferences(false, null, cancellationToken);

        internal static async Task<MimeMessage> CloneStreamReferencesAsync(this MimeMessage mimeMessage, bool persistent, MemoryBlockStream memoryBlockStream = null, CancellationToken cancellationToken = default)
        {
            if (memoryBlockStream == null)
                memoryBlockStream = new MemoryBlockStream();
            await mimeMessage.WriteToAsync(memoryBlockStream, cancellationToken);
            memoryBlockStream.Position = 0;
            var result = await MimeMessage.LoadAsync(memoryBlockStream, persistent, cancellationToken).ConfigureAwait(false);
            //await memoryBlockStream.FlushAsync().ConfigureAwait(false);
            // MemoryStream doesn't have any unmanaged resources, so it doesn't need to be disposed.
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
