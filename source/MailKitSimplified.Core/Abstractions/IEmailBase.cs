using System.Collections.Generic;

namespace MailKitSimplified.Core.Abstractions
{
    /// <summary>
    /// Simple email including standardised email envelope header information.
    /// <seealso href="https://datatracker.ietf.org/doc/html/rfc8621#section-4.1.2.3">RFC 8621</seealso>
    /// </summary>
    public interface IEmailBase
    {
        IList<IEmailAddress> From { get; set; }
        IList<IEmailAddress> To { get; set; }
        IList<IEmailAddress> Cc { get; set; }
        IList<IEmailAddress> Bcc { get; set; }
        string Subject { get; set; }
        string Body { get; set; }
        bool IsHtml { get; set; }
    }
}
