using System.Collections.Generic;

namespace MailKitSimplified.Generic.Abstractions
{
    /// <summary>
    /// Simple email format based on the RFC standards:
    /// <see href="https://www.rfc-editor.org/rfc/rfc8621">RFC 8621 (2019) JSON Meta Application Protocol</see>,
    /// <seealso href="https://www.rfc-editor.org/rfc/rfc5322">RFC 5322 (2008) Internet Message Format</seealso>,
    /// <seealso href="https://www.rfc-editor.org/rfc/rfc2822">RFC 2822 (2001) Internet Message Format</seealso>,
    /// <seealso href="https://www.rfc-editor.org/rfc/rfc822">RFC 822 (1982) ARPA Internet Text Messages</seealso>.
    /// </summary>
    public interface IGenericEmail
    {
        /// <summary>
        /// Custom email headers, prefixed with "X-"
        /// (<see href="https://www.rfc-editor.org/rfc/rfc822#section-4.7.4"/>).
        /// </summary>
        IDictionary<string, string> Headers { get; set; }

        /// <summary>
        /// Contacts to address the email from.
        /// </summary>
        IList<IGenericEmailContact> From { get; set; }

        /// <summary>
        /// Reply address for a contact other than the sender.
        /// </summary>
        IList<IGenericEmailContact> ReplyTo { get; set; }

        /// <summary>
        /// Contacts to address the email to.
        /// </summary>
        IList<IGenericEmailContact> To { get; set; }

        /// <summary>
        /// Contacts to carbon-copy the email to.
        /// </summary>
        IList<IGenericEmailContact> Cc { get; set; }

        /// <summary>
        /// Contacts to blind carbon-copy the email to.
        /// </summary>
        IList<IGenericEmailContact> Bcc { get; set; }

        /// <summary>
        /// Attachments send with the email.
        /// </summary>
        IDictionary<string, object> Attachments { get; set; }

        /// <summary>
        /// Email subject.
        /// </summary>
        string Subject { get; set; }

        /// <summary>
        /// Optional plain-text version of the email body (text/plain).
        /// </summary>
        string BodyText { get; set; }

        /// <summary>
        /// HTML-formatted body of the email (text/html).
        /// </summary>
        string BodyHtml { get; set; }

        /// <summary>
        /// Email envelope (from, to, subject etc.) in a readable format.
        /// </summary>
        /// <returns>Email summary in plain text.</returns>
        string ToString();
    }
}
