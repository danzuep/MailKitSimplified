namespace MailKitSimplified.Core.Abstractions
{
    public interface IBasicEmailWriter
    {
        /// <summary>
        /// Add a custom header to the email, prefixed with "X-"
        /// (<see href="https://www.rfc-editor.org/rfc/rfc822#section-4.7.4"/>).
        /// </summary>
        /// <param name="key">Header key ("X-FieldName").</param>
        /// <param name="value">Header value.</param>
        /// <returns><see cref="IBasicEmailWriter"/> interface.</returns>
        IBasicEmailWriter Header(string key, string value);

        /// <summary>
        /// Add a sender's details to the email.
        /// </summary>
        /// <param name="emailAddress">Email address of sender.</param>
        /// <param name="name">Name of sender.</param>
        /// <returns><see cref="IBasicEmailWriter"/> interface.</returns>
        IBasicEmailWriter From(string emailAddress, string name = null);

        /// <summary>
        /// Add a recipient to the email.
        /// </summary>
        /// <param name="emailAddress">Email address of recipient.</param>
        /// <param name="name">Name of recipient.</param>
        /// <returns><see cref="IBasicEmailWriter"/> interface.</returns>
        IBasicEmailWriter To(string emailAddress, string name = null);

        /// <summary>
        /// Add a carbon-copy recipient to the email.
        /// </summary>
        /// <param name="emailAddress">Email address of recipient.</param>
        /// <param name="name">Name of recipient.</param>
        /// <returns><see cref="IBasicEmailWriter"/> interface.</returns>
        IBasicEmailWriter Cc(string emailAddress, string name = null);

        /// <summary>
        /// Add a blind-carbon-copy recipient to the email.
        /// </summary>
        /// <param name="emailAddress">Email address of recipient.</param>
        /// <param name="name">Name of recipient.</param>
        /// <returns><see cref="IBasicEmailWriter"/> interface.</returns>
        IBasicEmailWriter Bcc(string emailAddress, string name = null);

        /// <summary>
        /// Sets or overwrites the subject of the email.
        /// </summary>
        /// <param name="subject">Email subject.</param>
        /// <returns><see cref="IBasicEmailWriter"/> interface.</returns>
        IBasicEmailWriter Subject(string subject);

        /// <summary>
        /// Quote the original subject of the email.
        /// </summary>
        /// <param name="prefix">Prepend to subject.</param>
        /// <param name="suffix">Append to subject.</param>
        /// <returns><see cref="IBasicEmailWriter"/> interface.</returns>
        IBasicEmailWriter Subject(string prefix, string suffix);

        /// <summary>
        /// Add an object as an attachment to the email.
        /// </summary>
        /// <param name="key">Attachment key (Content-Id).</param>
        /// <param name="value">Attachemnt object value.</param>
        /// <returns><see cref="IBasicEmailWriter"/> interface.</returns>
        IBasicEmailWriter Attach(string key, object value);

        /// <summary>
        /// Add a plain-text body to the email.
        /// </summary>
        /// <param name="textPlain">Body content as text/plain.</param>
        /// <returns><see cref="IBasicEmailWriter"/> interface.</returns>
        IBasicEmailWriter BodyText(string textPlain);

        /// <summary>
        /// Add a HTML-formatted body to the email.
        /// </summary>
        /// <param name="textHtml">Body content as text/html.</param>
        /// <returns><see cref="IBasicEmailWriter"/> interface.</returns>
        IBasicEmailWriter BodyHtml(string textHtml);

        /// <summary>
        /// Copy this email writer to re-use it as a template.
        /// </summary>
        /// <returns>Shallow copy of this email writer.</returns>
        IBasicEmailWriter Copy();

        /// <summary>
        /// Get the email built as an <see cref="IBasicEmail"/>.
        /// </summary>
        IBasicEmail Result { get; }
    }
}
