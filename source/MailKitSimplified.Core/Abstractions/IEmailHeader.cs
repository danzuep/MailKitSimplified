using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace MailKitSimplified.Core.Abstractions
{
    /// <summary>
    /// Standardised email header format.
    /// <seealso href="https://datatracker.ietf.org/doc/html/rfc8621#section-4.1.2.3">RFC 8621</seealso>
    /// </summary>
    public interface IEmailHeader
    {
        IList<IEmailAddress> From { get; set; }
        //IList<IEmailAddress> Sender { get; set; }
        //IList<IEmailAddress> ReplyTo { get; set; }
        IList<IEmailAddress> To { get; set; }
        //IList<IEmailAddress> Cc { get; set; }
        //IList<IEmailAddress> Bcc { get; set; }
        //IList<IEmailAddress> ResentFrom { get; set; }
        //IList<IEmailAddress> ResentSender { get; set; }
        //IList<IEmailAddress> ResentReplyTo { get; set; }
        //IList<IEmailAddress> ResentTo { get; set; }
        //IList<IEmailAddress> ResentCc { get; set; }
        //IList<IEmailAddress> ResentBcc { get; set; }
    }
}
