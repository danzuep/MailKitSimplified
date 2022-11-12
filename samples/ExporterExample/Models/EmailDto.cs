using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using ExporterExample.Abstractions;

namespace ExporterExample.Models
{
    public class EmailDto : IEmailDto
    {
        public DateTimeOffset? Date { get; set; } = DateTimeOffset.Now;
        [Required]
        public IEnumerable<string> From { get; set; } = new List<string>();
        public IEnumerable<string> To { get; set; } = new List<string>();
        public IEnumerable<string> Cc { get; set; } = new List<string>();
        public IEnumerable<string> Bcc { get; set; } = new List<string>();
        public IEnumerable<string> AttachmentNames { get; set; } = new List<string>();
        public string MessageId { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string BodyText { get; set; } = string.Empty;

        [ExcludeFromCodeCoverage]
        public override string ToString()
        {
            string envelope = string.Empty;
            using (var text = new StringWriter())
            {
                var attachmentCount = AttachmentNames.Count();
                text.WriteLine("From: {0}", string.Join(";", From));
                text.WriteLine("To: {0}", string.Join(";", To));
                text.WriteLine("Cc: {0}", string.Join(";", Cc));
                text.WriteLine("Bcc: {0}", string.Join(";", Bcc));
                if (attachmentCount > 0)
                    text.WriteLine("{0} Attachment{1}: {2}",
                        attachmentCount,
                        attachmentCount == 1 ? "" : "s",
                        string.Join(";", AttachmentNames));
                text.WriteLine("Subject: {0}", Subject);
                envelope = text.ToString();
            }
            return envelope;
        }
    }
}
