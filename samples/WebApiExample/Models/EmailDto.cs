using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using WebApiExample.Abstractions;

namespace WebApiExample.Models
{
    public class EmailDto : IEmailDto
    {
        [Required]
        public IEnumerable<IEmailAddressDto> From { get; set; } = new List<IEmailAddressDto>();
        public IEnumerable<IEmailAddressDto> To { get; set; } = new List<IEmailAddressDto>();
        public IEnumerable<string> AttachmentNames { get; set; } = new List<string>();
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
