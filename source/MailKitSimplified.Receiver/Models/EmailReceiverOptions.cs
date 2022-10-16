using System;
using System.Net;
using System.ComponentModel.DataAnnotations;

namespace MailKitSimplified.Receiver.Models
{
    public class EmailReceiverOptions
    {
        public const string SectionName = "EmailReceiver";

        [Required]
        public string ImapHost { get; set; }
        public ushort ImapPort { get; set; }
        public NetworkCredential ImapCredential { get; set; }
        public string MailFolderName { get; set; } = "INBOX";
        public string ProtocolLog { get; set; } = null; // "C:/Temp/EmailClientImap.log";
        // public string DownloadPath { get; set; } = "C:/Temp/Emails";

        public EmailReceiverOptions() { }

        public EmailReceiverOptions(string imapHost, NetworkCredential imapCredential, ushort imapPort = 0, string protocolLog = null)
        {
            if (string.IsNullOrWhiteSpace(imapHost))
                throw new ArgumentNullException(nameof(imapHost));

            ImapHost = imapHost;
            ImapPort = imapPort;
            ImapCredential = imapCredential;
            ProtocolLog = protocolLog;
        }
    }
}
