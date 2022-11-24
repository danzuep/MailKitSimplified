using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MailKitSimplified.Core.Abstractions
{
    public interface ISendableEmailWriter
    {
        /// <summary>
        /// Add a sender's details to the email.
        /// </summary>
        /// <param name="emailAddress">Email address of sender.</param>
        /// <param name="name">Name of sender.</param>
        /// <returns><see cref="ISendableEmailWriter"/> interface.</returns>
        ISendableEmailWriter From(string emailAddress, string name = null);

        /// <summary>
        /// Specify a reply contact other than the sender.
        /// </summary>
        /// <param name="emailAddress">Reply email address.</param>
        /// <param name="name">Name of reply contact.</param>
        /// <returns><see cref="ISendableEmailWriter"/> interface.</returns>
        ISendableEmailWriter ReplyTo(string name, string emailAddress);

        /// <summary>
        /// Add a recipient to the email.
        /// </summary>
        /// <param name="emailAddress">Email address of recipient.</param>
        /// <param name="name">Name of recipient.</param>
        /// <returns><see cref="ISendableEmailWriter"/> interface.</returns>
        ISendableEmailWriter To(string emailAddress, string name = null);

        /// <summary>
        /// Add a carbon-copy recipient to the email.
        /// </summary>
        /// <param name="emailAddress">Email address of recipient.</param>
        /// <param name="name">Name of recipient.</param>
        /// <returns><see cref="ISendableEmailWriter"/> interface.</returns>
        ISendableEmailWriter Cc(string emailAddress, string name = null);

        /// <summary>
        /// Add a blind-carbon-copy recipient to the email.
        /// </summary>
        /// <param name="emailAddress">Email address of recipient.</param>
        /// <param name="name">Name of recipient.</param>
        /// <returns><see cref="ISendableEmailWriter"/> interface.</returns>
        ISendableEmailWriter Bcc(string emailAddress, string name = null);

        /// <summary>
        /// Sets or overwrites the subject of the email.
        /// </summary>
        /// <param name="subject">Email subject.</param>
        /// <returns><see cref="ISendableEmailWriter"/> interface.</returns>
        ISendableEmailWriter Subject(string subject);

        /// <summary>
        /// Quote the original subject of the email.
        /// </summary>
        /// <param name="prefix">Prepend to subject.</param>
        /// <param name="suffix">Append to subject.</param>
        /// <returns><see cref="ISendableEmailWriter"/> interface.</returns>
        ISendableEmailWriter Subject(string prefix, string suffix);

        /// <summary>
        /// Add a plain-text body to the email.
        /// </summary>
        /// <param name="textPlain">Body content as text/plain.</param>
        /// <returns><see cref="ISendableEmailWriter"/> interface.</returns>
        ISendableEmailWriter BodyText(string textPlain);

        /// <summary>
        /// Add a HTML-formatted body to the email.
        /// </summary>
        /// <param name="textHtml">Body content as text/html.</param>
        /// <returns><see cref="ISendableEmailWriter"/> interface.</returns>
        ISendableEmailWriter BodyHtml(string textHtml);

        /// <summary>
        /// Add a file attachment to the email.
        /// </summary>
        /// <param name="filePath">File to attach.</param>
        /// <returns><see cref="ISendableEmailWriter"/> interface.</returns>
        ISendableEmailWriter Attach(string filePath);

        /// <summary>
        /// Add a stream attachment to the email.
        /// </summary>
        /// <param name="stream">Stream to attach.</param>
        /// <param name="contentId">Content ID or attachment name.</param>
        /// <returns><see cref="ISendableEmailWriter"/> interface.</returns>
        ISendableEmailWriter Attach(Stream stream, string contentId);

        /// <summary>
        /// Add a custom header to the email, prefixed with "X-"
        /// (<see href="https://www.rfc-editor.org/rfc/rfc822#section-4.7.4"/>).
        /// </summary>
        /// <param name="key">Header key ("X-FieldName").</param>
        /// <param name="value">Header value.</param>
        /// <returns><see cref="ISendableEmailWriter"/> interface.</returns>
        ISendableEmailWriter Header(string key, string value);

        /// <summary>
        /// Copy this email writer to re-use it as a template.
        /// </summary>
        /// <returns>Shallow copy of this email writer.</returns>
        ISendableEmailWriter Copy();

        /// <summary>
        /// Get the email built as an <see cref="ISendableEmail"/>.
        /// </summary>
        ISendableEmail Result { get; }

        /// <summary>
        /// Send the email asynchronously and reset the email.
        /// </summary>
        /// <param name="cancellationToken">Stop the email from sending.</param>
        Task SendAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Attempt to send the email asynchronously and reset the email.
        /// </summary>
        /// <param name="cancellationToken">Stop the email from sending.</param>
        /// <returns>True if the email sent successfully.</returns>
        Task<bool> TrySendAsync(CancellationToken cancellationToken = default);
    }
}
