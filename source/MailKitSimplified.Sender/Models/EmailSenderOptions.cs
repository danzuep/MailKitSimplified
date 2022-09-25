using System.ComponentModel.DataAnnotations;
using System.Net;

namespace MailKitSimplified.Sender.Models
{
    public class EmailSenderOptions
    {
        [Required]
        public string SmtpHost { get; set; }
        public NetworkCredential SmtpCredential { get; set; } = null;
        public string ProtocolLog { get; set; } = null;
    }
}
