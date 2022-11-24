using MimeKit;
using MailKit;
using MailKit.Net.Smtp;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MailKitSimplified.Sender.Abstractions
{
    /// <summary>
    /// Sends MailKit <see cref="MimeMessage"/> emails.
    /// </summary>
    public interface ISmtpSender : IDisposable
    {
        /// <summary>
        /// Write an email fluently with an <see cref="IEmailWriter"/>.
        /// </summary>
        IEmailWriter WriteEmail { get; }

        /// <summary>
        /// Connect and authenticate the SMTP client.
        /// </summary>
        /// <param name="cancellationToken">Stop connecting the client.</param>
        /// <returns>Connected <see cref="ISmtpClient">SMTP client</see>.</returns>
        ValueTask<ISmtpClient> ConnectSmtpClientAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Send the email asynchronously and reset the email.
        /// </summary>
        /// <param name="mimeMessage"><see cref="MimeMessage"/> to send.</param>
        /// <param name="cancellationToken">Stop the email from sending.</param>
        /// <param name="transferProgress">Current email sending progress.</param>
        Task SendAsync(MimeMessage mimeMessage, CancellationToken cancellationToken = default, ITransferProgress transferProgress = null);

        /// <summary>
        /// Attempt to send the email asynchronously and reset the email.
        /// </summary>
        /// <param name="mimeMessage"><see cref="MimeMessage"/> to send.</param>
        /// <param name="cancellationToken">Stop the email from sending.</param>
        /// <param name="transferProgress">Current email sending progress.</param>
        /// <returns>True if the email sent successfully.</returns>
        Task<bool> TrySendAsync(MimeMessage mimeMessage, CancellationToken cancellationToken = default, ITransferProgress transferProgress = null);
    }
}
