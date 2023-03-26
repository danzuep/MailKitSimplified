using System.Net;
using MailKitSimplified.Sender.Models;
using MailKitSimplified.Receiver.Models;

namespace MailKitSimplified.Email.Models
{
    internal sealed class EmailOptions
    {
        public const string SectionName = "Email";
        public static readonly string Localhost = "localhost";

        public string Host { get; set; } = string.Empty;

        public ushort Port { get; set; }

        public string Username { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;

        private NetworkCredential _credential =>
            new NetworkCredential(Username, Password);

        public EmailSenderOptions EmailSenderOptions =>
            new EmailSenderOptions(Host, _credential, Port);

        public EmailReceiverOptions EmailReceiverOptions =>
            new EmailReceiverOptions(Host, _credential, Port);
    }
}