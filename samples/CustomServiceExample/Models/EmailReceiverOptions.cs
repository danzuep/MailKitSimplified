using System.ComponentModel.DataAnnotations;
using System.Net;

namespace CustomServiceExample.Models
{
    public class EmailReceiverOptions
    {
        [Required]
        public string ImapHost { get; set; }
        [Required]
        public NetworkCredential ImapCredential { get; set; }
        public string FolderToProcess { get; set; } = "INBOX";
    }
}
