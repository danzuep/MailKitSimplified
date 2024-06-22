using System;
using System.Threading;
using System.Threading.Tasks;
using MailKit;

namespace MailKitSimplified.Receiver.Extensions
{
    public static class MailFolderExtensions
    {
        /// <summary>
        /// Get a mail subfolder if it exists, or create it if not.
        /// </summary>
        /// <param name="mailFolderFullName">Folder name to search for.</param>
        /// <param name="baseFolder">Base folder to search in, Inbox by default</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Mail folder with a matching name.</returns>
        public static async Task<IMailFolder> GetOrCreateSubfolderAsync(this IMailFolder baseFolder, string mailFolderFullName, CancellationToken cancellationToken = default)
        {
            if (baseFolder == null)
                throw new ArgumentNullException(nameof(baseFolder));
            IMailFolder mailFolder;
            try
            {
                mailFolder = await baseFolder.GetSubfolderAsync(mailFolderFullName, cancellationToken);
            }
            catch (FolderNotFoundException)
            {
                mailFolder = await baseFolder.CreateAsync(mailFolderFullName, isMessageFolder: true, cancellationToken).ConfigureAwait(false);
            }
            return mailFolder;
        }
    }
}
