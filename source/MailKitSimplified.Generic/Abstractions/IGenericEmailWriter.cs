using System.Threading;
using System.Threading.Tasks;

namespace MailKitSimplified.Core.Abstractions
{
    /// <summary>
    /// Interface for writing generic emails.
    /// </summary>
    public interface IGenericEmailWriter
    {
        /// <summary>
        /// Add a custom header to the email, prefixed with "X-"
        /// (<see href="https://www.rfc-editor.org/rfc/rfc822#section-4.7.4"/>).
        /// </summary>
        /// <param name="key">Header key ("X-FieldName").</param>
        /// <param name="value">Header value.</param>
        /// <returns><see cref="IGenericEmailWriter"/> interface.</returns>
        IGenericEmailWriter Header(string key, string value);

        /// <summary>
        /// Add a sender's details to the email.
        /// </summary>
        /// <param name="emailAddress">Email address of sender.</param>
        /// <param name="name">Name of sender.</param>
        /// <returns><see cref="IGenericEmailWriter"/> interface.</returns>
        IGenericEmailWriter From(string emailAddress, string name = null);

        /// <summary>
        /// Add a recipient to the email.
        /// </summary>
        /// <param name="emailAddress">Email address of recipient.</param>
        /// <param name="name">Name of recipient.</param>
        /// <returns><see cref="IGenericEmailWriter"/> interface.</returns>
        IGenericEmailWriter To(string emailAddress, string name = null);

        /// <summary>
        /// Add a carbon-copy recipient to the email.
        /// </summary>
        /// <param name="emailAddress">Email address of recipient.</param>
        /// <param name="name">Name of recipient.</param>
        /// <returns><see cref="IGenericEmailWriter"/> interface.</returns>
        IGenericEmailWriter Cc(string emailAddress, string name = null);

        /// <summary>
        /// Add a blind-carbon-copy recipient to the email.
        /// </summary>
        /// <param name="emailAddress">Email address of recipient.</param>
        /// <param name="name">Name of recipient.</param>
        /// <returns><see cref="IGenericEmailWriter"/> interface.</returns>
        IGenericEmailWriter Bcc(string emailAddress, string name = null);

        /// <summary>
        /// Sets or overwrites the subject of the email.
        /// </summary>
        /// <param name="subject">Email subject.</param>
        /// <returns><see cref="IGenericEmailWriter"/> interface.</returns>
        IGenericEmailWriter Subject(string subject);

        /// <summary>
        /// Quote the original subject of the email.
        /// </summary>
        /// <param name="prefix">Prepend to subject.</param>
        /// <param name="suffix">Append to subject.</param>
        /// <returns><see cref="IGenericEmailWriter"/> interface.</returns>
        IGenericEmailWriter Subject(string prefix, string suffix);

        /// <summary>
        /// Add an object as an attachment to the email.
        /// </summary>
        /// <param name="key">Attachment key (Content-Id).</param>
        /// <param name="value">Attachemnt object value.</param>
        /// <returns><see cref="IGenericEmailWriter"/> interface.</returns>
        IGenericEmailWriter Attach(string key, object value);

        /// <summary>
        /// Add a plain-text body to the email.
        /// </summary>
        /// <param name="textPlain">Body content as text/plain.</param>
        /// <returns><see cref="IGenericEmailWriter"/> interface.</returns>
        IGenericEmailWriter BodyText(string textPlain);

        /// <summary>
        /// Add a HTML-formatted body to the email.
        /// </summary>
        /// <param name="textHtml">Body content as text/html.</param>
        /// <returns><see cref="IGenericEmailWriter"/> interface.</returns>
        IGenericEmailWriter BodyHtml(string textHtml);

        /// <summary>
        /// Copy this email writer to re-use it as a template.
        /// </summary>
        /// <returns>Shallow copy of this email writer.</returns>
        IGenericEmailWriter Copy();

        /// <summary>
        /// Get the email built as an <see cref="IGenericEmail"/>.
        /// </summary>
        IGenericEmail AsEmail { get; }

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
