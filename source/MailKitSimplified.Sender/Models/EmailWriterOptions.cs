using MimeKit;

namespace MailKitSimplified.Sender.Models
{
    public class EmailWriterOptions
    {
        public const string SectionName = "EmailWriter";

        public MailboxAddress DefaultFrom { get; set; } = null;
        public MailboxAddress DefaultReplyTo { get; set; } = new MailboxAddress("Unmonitored", "noreply@localhost");
        public bool GenerateGuidIfFromNotSet { get; set; } = true;
    }
}
