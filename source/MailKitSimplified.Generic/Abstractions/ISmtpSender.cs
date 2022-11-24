using System;
using System.Threading;
using System.Threading.Tasks;

namespace MailKitSimplified.Core.Abstractions
{
    public interface ISmtpSender : IDisposable
    {
        /// <summary>
        /// Write an email fluently with an <see cref="ISendableEmailWriter"/>.
        /// </summary>
        ISendableEmailWriter WriteEmail { get; }

        /// <summary>
        /// Send the email asynchronously.
        /// </summary>
        /// <param name="email"><see cref="ISendableEmail"/> to send.</param>
        /// <param name="cancellationToken">Stop the email from sending.</param>
        /// <returns>True if the email sent successfully.</returns>
        Task SendAsync(ISendableEmail email, CancellationToken cancellationToken = default);
    }
}
