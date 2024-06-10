using MailKit;
using MailKit.Net.Imap;
using MailKit.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace MailKitSimplified.Receiver.Extensions
{
    public static class ImapClientExtensions
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

#if NET5_0_OR_GREATER
        private static async IAsyncEnumerable<IMailFolder> GetAllSubfoldersAsync(this IImapClient imapClient, FolderNamespace folderNamespace, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var folders = await imapClient.GetFoldersAsync(folderNamespace, subscribedOnly: false, cancellationToken).ConfigureAwait(false);
            foreach (var folder in folders)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var subfolders = await folder.GetSubfoldersAsync(subscribedOnly: false, cancellationToken).ConfigureAwait(false);
                foreach (var subfolder in subfolders)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    yield return subfolder;
                }
            }
        }

        internal static async IAsyncEnumerable<IMailFolder> GetAllSubfoldersAsync(this IImapClient imapClient, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (imapClient == null)
                throw new ArgumentNullException(nameof(imapClient));
            foreach (var folderNamespace in imapClient.PersonalNamespaces)
            {
                await foreach (var folder in imapClient.GetAllSubfoldersAsync(folderNamespace).WithCancellation(cancellationToken).ConfigureAwait(false))
                {
                    yield return folder;
                }
            }
            foreach (var folderNamespace in imapClient.SharedNamespaces)
            {
                await foreach (var folder in imapClient.GetAllSubfoldersAsync(folderNamespace).WithCancellation(cancellationToken).ConfigureAwait(false))
                {
                    yield return folder;
                }
            }
            foreach (var folderNamespace in imapClient.OtherNamespaces)
            {
                await foreach (var folder in imapClient.GetAllSubfoldersAsync(folderNamespace).WithCancellation(cancellationToken).ConfigureAwait(false))
                {
                    yield return folder;
                }
            }
        }
#else
        private static async Task<IList<IMailFolder>> GetAllSubfoldersAsync(this IImapClient imapClient, FolderNamespace folderNamespace, CancellationToken cancellationToken = default)
        {
            var results = new List<IMailFolder>();
            var folders = await imapClient.GetFoldersAsync(folderNamespace, subscribedOnly: false, cancellationToken).ConfigureAwait(false);
            foreach (var folder in folders)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var subfolders = await folder.GetSubfoldersAsync(subscribedOnly: false, cancellationToken).ConfigureAwait(false);
                results.AddRange(subfolders);
            }
            return results;
        }

        internal static async Task<IList<IMailFolder>> GetAllSubfoldersAsync(this IImapClient imapClient, CancellationToken cancellationToken = default)
        {
            if (imapClient == null)
                throw new ArgumentNullException(nameof(imapClient));
            var results = new List<IMailFolder>();
            foreach (var folderNamespace in imapClient.PersonalNamespaces)
            {
                var subfolders = await imapClient.GetAllSubfoldersAsync(folderNamespace, cancellationToken).ConfigureAwait(false);
                results.AddRange(subfolders);
            }
            foreach (var folderNamespace in imapClient.SharedNamespaces)
            {
                var subfolders = await imapClient.GetAllSubfoldersAsync(folderNamespace, cancellationToken).ConfigureAwait(false);
                results.AddRange(subfolders);
            }
            foreach (var folderNamespace in imapClient.OtherNamespaces)
            {
                var subfolders = await imapClient.GetAllSubfoldersAsync(folderNamespace, cancellationToken).ConfigureAwait(false);
                results.AddRange(subfolders);
            }
            return results;
        }
#endif
    }
}
