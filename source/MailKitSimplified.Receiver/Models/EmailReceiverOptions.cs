using System;
using System.Net;
using System.Linq;
using System.ComponentModel.DataAnnotations;

namespace MailKitSimplified.Receiver.Models
{
    public class EmailReceiverOptions
    {
        public const string SectionName = "EmailReceiver";

        [Required]
        public string ImapHost { get; set; }
        public ushort ImapPort { get; set; } = 0;
        public NetworkCredential ImapCredential { get; set; } = new NetworkCredential();
        public string ProtocolLog { get; set; } = null;
        public string MailFolderName { get; set; } = "INBOX";

        // Constructor required for Configuration mapping.
        public EmailReceiverOptions() { }

        public EmailReceiverOptions(string imapHost, NetworkCredential imapCredential = null, ushort imapPort = 0, string protocolLog = null, string mailFolderName = null)
        {
            if (string.IsNullOrWhiteSpace(imapHost))
                throw new ArgumentNullException(nameof(imapHost));
            var hostParts = imapPort == 0 ? imapHost.Split(':') : Array.Empty<string>();
            if (hostParts.Length == 2 && ushort.TryParse(hostParts.LastOrDefault(), out imapPort))
                imapHost = hostParts.FirstOrDefault();
            if (imapCredential != null && imapCredential.UserName == null)
                imapCredential.UserName = string.Empty;

            ImapHost = imapHost;
            ImapPort = imapPort;
            if (imapCredential != null)
                ImapCredential = imapCredential;
            ProtocolLog = protocolLog;
            if (!string.IsNullOrWhiteSpace(mailFolderName))
                MailFolderName = mailFolderName;
        }

        public override string ToString() => $"{ImapHost}:{ImapPort} {ImapCredential.UserName} {MailFolderName}";
    }
}
