using System;
using System.Net;
using System.ComponentModel.DataAnnotations;

namespace MailKitSimplified.Core.Models
{
    public class EmailSenderOptions
    {
        public const string SectionName = "EmailSender";

        [Required]
        public string SmtpHost { get; set; }
        public int SmtpPort { get; set; } = 0; // 25, or 587 for SSL
        public NetworkCredential SmtpCredential { get; set; } = null;
        public string ProtocolLog { get; set; } = null;

        public EmailSenderOptions() { }

        public EmailSenderOptions(string smtpHost, int smtpPort = 0, NetworkCredential smtpCredential = null, string protocolLog = null)
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
