using System.ComponentModel.DataAnnotations;

namespace MailKitSimplified.Generic.Models
{
    public class GenericSmtpOptions
    {
        public const string SectionName = "Smtp";

        [Required]
        public string Host { get; set; }
        public ushort Port { get; set; } = 0;
        public string Username { get; set; }
        public string Password { get; set; }

        public override string ToString() => $"{Host}:{Port}";
    }
}
