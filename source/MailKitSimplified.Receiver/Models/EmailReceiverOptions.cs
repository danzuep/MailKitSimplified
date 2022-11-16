using System;
using System.Net;
using System.ComponentModel.DataAnnotations;

namespace MailKitSimplified.Receiver.Models
{
    public class EmailReceiverOptions
    {
        public const string SectionName = "EmailReceiver";

        [Required]
        public string ImapHost { get; set; } // "localhost";
        public ushort ImapPort { get; set; } = 0;
        public NetworkCredential ImapCredential { get; set; }
        public string ProtocolLog { get; set; } = null; // @"C:/Temp/Email logs/ImapClient.txt";
        public string MailFolderName { get; set; } = "INBOX";
        // public string DownloadPath { get; set; } = "C:/Temp/Emails";

        public EmailReceiverOptions() { }

        public EmailReceiverOptions(string imapHost, NetworkCredential imapCredential = null, ushort imapPort = 0, string protocolLog = null, string mailFolderName = null)
        {
            if (string.IsNullOrWhiteSpace(imapHost))
                throw new ArgumentNullException(nameof(imapHost));

            ImapHost = imapHost;
            ImapPort = imapPort;
            ImapCredential = imapCredential ?? new NetworkCredential();
            ProtocolLog = protocolLog;
            if (!string.IsNullOrWhiteSpace(mailFolderName))
                MailFolderName = mailFolderName;
        }
    }
}
