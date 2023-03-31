using MailKit.Net.Imap;
using MailKit.Security;
using System.Threading;
using System.Threading.Tasks;

namespace MailKitSimplified.Receiver.Extensions
{
    public static class IImapClientExtensions
    {
        public static async ValueTask ConnectImapClientAsync(this IImapClient imapClient, string host, ushort port, CancellationToken cancellationToken = default)
        {
            if (!imapClient.IsConnected)
            {
                await imapClient.ConnectAsync(host, port, SecureSocketOptions.Auto, cancellationToken).ConfigureAwait(false);
                if (imapClient.Capabilities.HasFlag(ImapCapabilities.Compress))
                    await imapClient.CompressAsync(cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
