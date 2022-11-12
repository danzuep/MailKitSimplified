using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net.Mail;

namespace WebApiExample.Abstractions
{
    /// <summary>
    /// Standardised email header format.
    /// <seealso href="https://datatracker.ietf.org/doc/html/rfc8621#section-4.1.2.3">RFC 8621</seealso>
    /// </summary>
    public interface IEmailDto
    {
        IEnumerable<IEmailAddressDto> From { get; set; }
        IEnumerable<IEmailAddressDto> To { get; set; }
        IEnumerable<string> AttachmentNames { get; set; }
        string Subject { get; set; }
        string BodyText { get; set; }
    }
}
