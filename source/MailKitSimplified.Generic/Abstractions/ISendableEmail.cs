using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace MailKitSimplified.Core.Abstractions
{
    public interface ISendableEmail : IBasicEmail
    {
        /// <summary>
        /// Reply address for a contact other than the sender.
        /// </summary>
        IList<IEmailContact> ReplyTo { get; set; }

        /// <summary>
        /// List of file paths to attach before sending.
        /// </summary>
        IList<string> AttachmentFilePaths { get; set; }

        /// <summary>
        /// Send the email asynchronously.
        /// </summary>
        /// <param name="cancellationToken">Stop the email from sending.</param>
        Task SendAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Attempt to send the email asynchronously.
        /// </summary>
        /// <param name="cancellationToken">Stop the email from sending.</param>
        /// <returns>True if the email sent successfully.</returns>
        Task<bool> TrySendAsync(CancellationToken cancellationToken = default);
    }
}
