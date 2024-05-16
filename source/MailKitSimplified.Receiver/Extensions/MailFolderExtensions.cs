using MailKit;
using System.Threading.Tasks;
using System.Threading;

namespace MailKitSimplified.Receiver.Extensions
{
    public static class MailFolderExtensions
    {
        /// <summary>
        /// Get a mail subfolder if it exists, or create it if not.
        /// </summary>
        /// <param name="mailFolderName">Folder name to search for.</param>
        /// <param name="baseFolder">Base folder to search in, Inbox by default</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Mail folder with a matching name.</returns>
        public static async Task<IMailFolder> GetOrCreateSubfolderAsync(this IMailFolder baseFolder, string mailFolderName, CancellationToken cancellationToken = default)
        {
            IMailFolder mailFolder;
            try
            {
                mailFolder = await baseFolder.GetSubfolderAsync(mailFolderName, cancellationToken);
            }
            catch (FolderNotFoundException)
            {
                mailFolder = await baseFolder.CreateAsync(mailFolderName, isMessageFolder: true, cancellationToken);
            }
            return mailFolder;
        }
    }
}
