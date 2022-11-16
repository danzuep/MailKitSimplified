using System;
using System.Net;
using System.ComponentModel.DataAnnotations;

namespace MailKitSimplified.Sender.Models
{
    public class EmailSenderOptions
    {
        public const string SectionName = "EmailSender";

        [Required]
        public string SmtpHost { get; set; } // "localhost";
        public ushort SmtpPort { get; set; } = 0; // 25, or 587 for SSL
        public NetworkCredential SmtpCredential { get; set; } = null;
        public string ProtocolLog { get; set; } = null; // @"C:/Temp/Email logs/SmtpClient.txt";
        //public string UploadPath { get; set; } = "C:/Temp/Emails/";

        public EmailSenderOptions() { }

        public EmailSenderOptions(string smtpHost, NetworkCredential smtpCredential = null, ushort smtpPort = 0, string protocolLog = null)
        {
            if (string.IsNullOrWhiteSpace(smtpHost))
                throw new ArgumentNullException(nameof(smtpHost));
            if (smtpCredential != null && smtpCredential.UserName == null && smtpCredential.Password == null)
                smtpCredential = null;

            SmtpHost = smtpHost;
            SmtpPort = smtpPort;
            SmtpCredential = smtpCredential; // ?? new NetworkCredential();
            ProtocolLog = protocolLog;
        }
    }
}
