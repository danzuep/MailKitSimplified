using System;
using System.Threading;
using System.Threading.Tasks;

namespace MailKitSimplified.Core.Abstractions
{
    /// <summary>
    /// Interface for sending generic emails.
    /// </summary>
    public interface IGenericEmailSender : IDisposable
    {
        /// <summary>
        /// Write an email fluently with an <see cref="IGenericEmailWriter"/>.
        /// </summary>
        IGenericEmailWriter WriteEmail { get; }

        /// <summary>
        /// Send the email asynchronously.
        /// </summary>
        /// <param name="email"><see cref="IGenericEmail"/> to send.</param>
        /// <param name="cancellationToken">Stop the email from sending.</param>
        Task SendAsync(IGenericEmail email, CancellationToken cancellationToken = default);

        /// <summary>
        /// Attempt to send the email asynchronously.
        /// </summary>
        /// <param name="email"><see cref="IGenericEmail"/> to send.</param>
        /// <param name="cancellationToken">Stop the email from sending.</param>
        /// <returns>True if the email sent successfully.</returns>
        Task<bool> TrySendAsync(IGenericEmail email, CancellationToken cancellationToken = default);
    }
}
