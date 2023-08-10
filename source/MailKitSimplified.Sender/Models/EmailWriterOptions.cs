namespace MailKitSimplified.Sender.Models
{
    public class EmailWriterOptions
    {
        public const string SectionName = "EmailWriter";

        public string TemplateFilePath { get; set; } = null;
        public bool GenerateDefaultFromAddress { get; set; } = true;
        public string DefaultReplyToAddress { get; set; } = "noreply@localhost";
    }
}
