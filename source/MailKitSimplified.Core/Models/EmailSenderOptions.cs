using System;
using System.Net;
using System.ComponentModel.DataAnnotations;

namespace MailKitSimplified.Core.Models
{
    public class EmailSenderOptions
    {
        public const string SectionName = "EmailSender";

        [Required]
        public string SmtpHost { get; set; } // "localhost";
        public ushort SmtpPort { get; set; } = 0; // 25, or 587 for SSL
        public NetworkCredential SmtpCredential { get; set; } = null;
        public string ProtocolLog { get; set; } = null; // "C:/Temp/EmailClientSmtp.log";
        //public string UploadPath { get; set; } = "C:/Temp/Emails/";

        public EmailSenderOptions() { }

        public EmailSenderOptions(string smtpHost, NetworkCredential smtpCredential = null, ushort smtpPort = 0, string protocolLog = null)
        {
            if (string.IsNullOrWhiteSpace(smtpHost))
                throw new ArgumentNullException(nameof(smtpHost));

            SmtpHost = smtpHost;
            SmtpPort = smtpPort;
            SmtpCredential = smtpCredential;
            ProtocolLog = protocolLog;
        }
    }
}
