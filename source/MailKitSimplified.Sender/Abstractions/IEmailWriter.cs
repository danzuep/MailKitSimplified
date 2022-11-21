using MimeKit;
using MailKit;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace MailKitSimplified.Sender.Abstractions
{
    /// <summary>
    /// <see href="https://datatracker.ietf.org/doc/html/rfc8621">RFC 8621 (2019) JSON Meta Application Protocol</see>
    /// <seealso href="https://datatracker.ietf.org/doc/html/rfc5322">RFC 5322 (2008) Internet Message Format</seealso>
    /// <seealso href="https://datatracker.ietf.org/doc/html/rfc2822">RFC 2822 (2001) Internet Message Format</seealso>
    /// <seealso href="https://datatracker.ietf.org/doc/html/rfc822">RFC 822 (1982) ARPA Internet Text Messages</seealso>
    /// </summary>
    public interface IEmailWriter
    {
        /// <summary>
        /// Add a sender's details to the email.
        /// </summary>
        /// <param name="name">Name of sender.</param>
        /// <param name="emailAddress">Email address of sender.</param>
        /// <returns><see cref="IEmailWriter"/> interface.</returns>
        IEmailWriter From(string name, string emailAddress);

        /// <summary>
        /// Add a senders email address to the email.
        /// </summary>
        /// <param name="emailAddress">Email address of sender.</param>
        /// <returns><see cref="IEmailWriter"/> interface.</returns>
        IEmailWriter From(string emailAddress);

        /// <summary>
        /// Override From contact(s) with a reply contact.
        /// </summary>
        /// <param name="name">Name of reply contact.</param>
        /// <param name="emailAddress">Reply email address.</param>
        /// <returns><see cref="IEmailWriter"/> interface.</returns>
        IEmailWriter ReplyTo(string name, string emailAddress);

        /// <summary>
        /// Override From address(es) with a reply address.
        /// </summary>
        /// <param name="emailAddress">Reply email address.</param>
        /// <returns><see cref="IEmailWriter"/> interface.</returns>
        IEmailWriter ReplyTo(string emailAddress);

        /// <summary>
        /// Add a recipient to the email.
        /// </summary>
        /// <param name="name">Name of recipient.</param>
        /// <param name="emailAddress">Email address of recipient.</param>
        /// <returns><see cref="IEmailWriter"/> interface.</returns>
        IEmailWriter To(string name, string emailAddress);

        /// <summary>
        /// Add recipient(s) to the email.
        /// Email addresses are split on ';', ',', ' ', '&', '|'.
        /// Names are automatically parsed from the email address(es).
        /// </summary>
        /// <param name="emailAddress">Email address(es) of recipient(s).</param>
        /// <returns><see cref="IEmailWriter"/> interface.</returns>
        IEmailWriter To(string emailAddress);

        /// <summary>
        /// Add a carbon-copy recipient to the email.
        /// </summary>
        /// <param name="name">Name of recipient.</param>
        /// <param name="emailAddress">Email address of recipient.</param>
        /// <returns><see cref="IEmailWriter"/> interface.</returns>
        IEmailWriter Cc(string name, string emailAddress);

        /// <summary>
        /// Add carbon-copy recipient(s) to the email.
        /// Email addresses are split on ';', ',', ' ', '&', '|'.
        /// Names are automatically parsed from the email address(es).
        /// </summary>
        /// <param name="emailAddress">Email address(es) of recipient(s).</param>
        /// <returns><see cref="IEmailWriter"/> interface.</returns>
        IEmailWriter Cc(string emailAddress);

        /// <summary>
        /// Add a blind-carbon-copy recipient to the email.
        /// </summary>
        /// <param name="name">Name of recipient.</param>
        /// <param name="emailAddress">Email address of recipient.</param>
        /// <returns><see cref="IEmailWriter"/> interface.</returns>
        IEmailWriter Bcc(string name, string emailAddress);

        /// <summary>
        /// Add blind-carbon-copy recipient(s) to the email.
        /// Email addresses are split on ';', ',', ' ', '&', '|'.
        /// Names are automatically parsed from the email address(es).
        /// </summary>
        /// <param name="emailAddress">Email address(es) of recipient(s).</param>
        /// <returns><see cref="IEmailWriter"/> interface.</returns>
        IEmailWriter Bcc(string emailAddress);

        /// <summary>
        /// Sets or overwrites the subject of the email.
        /// </summary>
        /// <param name="subject">Email subject.</param>
        /// <returns><see cref="IEmailWriter"/> interface.</returns>
        IEmailWriter Subject(string subject);

        /// <summary>
        /// Quote the original subject of the email.
        /// </summary>
        /// <param name="prefix">Prepend to subject.</param>
        /// <param name="suffix">Append to subject.</param>
        /// <returns><see cref="IEmailWriter"/> interface.</returns>
        IEmailWriter Subject(string prefix, string suffix);

        /// <summary>
        /// Add a plain-text body to the email.
        /// </summary>
        /// <param name="textHtml">Body content as text/plain.</param>
        /// <returns><see cref="IEmailWriter"/> interface.</returns>
        IEmailWriter BodyText(string textPlain);

        /// <summary>
        /// Add a HTML-formatted body to the email.
        /// </summary>
        /// <param name="textHtml">Body content as text/html.</param>
        /// <returns><see cref="IEmailWriter"/> interface.</returns>
        IEmailWriter BodyHtml(string textHtml);

        /// <summary>
        /// Add file attachments to the email.
        /// </summary>
        /// <param name="filePaths">Files to attach.</param>
        /// <returns><see cref="IEmailWriter"/> interface.</returns>
        IEmailWriter Attach(params string[] filePaths);

        /// <summary>
        /// Attempt to add file attachments to the email.
        /// </summary>
        /// <param name="filePaths">Files to attach.</param>
        /// <returns><see cref="IEmailWriter"/> interface.</returns>
        IEmailWriter TryAttach(params string[] filePaths);

        /// <summary>
        /// Attaches a stream to the email.
        /// </summary>
        /// <param name="stream">Stream to attach.</param>
        /// <param name="fileName">Attachment name.</param>
        /// <param name="contentType">Override for the content type.</param>
        /// <param name="contentId">Override for the content ID.</param>
        /// <param name="linkedResource">Inline or attached.</param>
        /// <returns><see cref="IEmailWriter"/> interface.</returns>
        IEmailWriter Attach(Stream stream, string fileName, string contentType = null, string contentId = null, bool linkedResource = false);

        /// <summary>
        /// Add an attachment to the email.
        /// </summary>
        /// <param name="mimeEntity">Entity to attach.</param>
        /// <param name="linkedResource">Inline or attached.</param>
        /// <returns><see cref="IEmailWriter"/> interface.</returns>
        IEmailWriter Attach(MimeEntity mimeEntity, bool linkedResource = false);

        /// <summary>
        /// Add a multiple attachments to the email.
        /// </summary>
        /// <param name="mimeEntities">Entities to attach.</param>
        /// <param name="linkedResource">Inline or attached.</param>
        /// <returns><see cref="IEmailWriter"/> interface.</returns>
        IEmailWriter Attach(IEnumerable<MimeEntity> mimeEntities, bool linkedResource = false);

        /// <summary>
        /// Add a custom header to the email, prefixed with "X-".
        /// <see href="https://datatracker.ietf.org/doc/html/rfc822#section-4.7.4"/>
        /// </summary>
        /// <param name="key">Header key ("X-FieldName").</param>
        /// <param name="value">Header value.</param>
        /// <returns><see cref="IEmailWriter"/> interface.</returns>
        IEmailWriter Header(string key, string value);

        /// <summary>
        /// Override the priority assigned to the email.
        /// </summary>
        /// <param name="priority"><see cref="MessagePriority"/>.</param>
        /// <returns><see cref="IEmailWriter"/> interface.</returns>
        IEmailWriter Priority(MessagePriority priority);

        /// <summary>
        /// Get the email built as a <see cref="MimeKit.MimeMessage"/>.
        /// </summary>
        MimeMessage MimeMessage { get; }

        /// <summary>
        /// Synchronous overload of SendAsync.
        /// </summary>
        /// <param name="cancellationToken">Stop the email from sending.</param>
        void Send(CancellationToken cancellationToken = default);

        /// <summary>
        /// Synchronous overload of TrySendAsync.
        /// </summary>
        /// <param name="cancellationToken">Stop the email from sending.</param>
        bool TrySend(CancellationToken cancellationToken = default);

        /// <summary>
        /// Send the email asynchronously and reset the email.
        /// </summary>
        /// <param name="cancellationToken">Stop the email from sending.</param>
        /// <param name="transferProgress">Current email upload progress.</param>
        Task SendAsync(CancellationToken cancellationToken = default, ITransferProgress transferProgress = null);

        /// <summary>
        /// Attempt to send the email asynchronously and reset the email.
        /// </summary>
        /// <param name="cancellationToken">Stop the email from sending.</param>
        /// <param name="transferProgress">Current email upload progress.</param>
        /// <returns>True if the email sent successfully.</returns>
        Task<bool> TrySendAsync(CancellationToken cancellationToken = default, ITransferProgress transferProgress = null);

        /// <summary>
        /// All emails details in an easy-to-read format.
        /// </summary>
        /// <returns>Plain-text email summary.</returns>
        string ToString();
    }
}
