using System.ComponentModel.DataAnnotations;
using System.Net;

namespace CustomServiceExample.Models
{
    public class EmailSenderOptions
    {
        [Required]
        public string SmtpHost { get; set; }
        public NetworkCredential SmtpCredential { get; set; } = null;
        public string ProtocolLogger { get; set; } = null;
    }
}
