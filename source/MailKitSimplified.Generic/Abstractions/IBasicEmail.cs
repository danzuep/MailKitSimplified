using System.Collections.Generic;

namespace MailKitSimplified.Core.Abstractions
{
    /// <summary>
    /// Simple email including standardised email envelope header information.
    /// <seealso href="https://www.rfc-editor.org/rfc/rfc8621#section-4.1.2.3">RFC 8621</seealso>
    /// </summary>
    public interface IBasicEmail
    {
        /// <summary>
        /// Custom email headers, prefixed with "X-"
        /// (<see href="https://www.rfc-editor.org/rfc/rfc822#section-4.7.4"/>).
        /// </summary>
        IDictionary<string, string> Headers { get; set; }

        /// <summary>
        /// Contacts to address the email from.
        /// </summary>
        IList<IEmailContact> From { get; set; }

        /// <summary>
        /// Contacts to address the email to.
        /// </summary>
        IList<IEmailContact> To { get; set; }

        /// <summary>
        /// Contacts to carbon-copy the email to.
        /// </summary>
        IList<IEmailContact> Cc { get; set; }

        /// <summary>
        /// Contacts to blind carbon-copy the email to.
        /// </summary>
        IList<IEmailContact> Bcc { get; set; }

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
    }
}
