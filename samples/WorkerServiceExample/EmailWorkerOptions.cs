namespace MailKitSimplified.Sender.Models
{
    public class EmailWorkerOptions
    {
        public const string SectionName = "EmailWorker";

        public string DefaultFromAddress { get; set; } = "noreply@localhost";
        public string DefaultToAddress { get; set; } = "noreply@localhost";
    }
}
