using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using MailKitSimplified.Sender.Abstractions;
using MailKitSimplified.Sender.Extensions;
using MailKitSimplified.Sender.Models;

namespace MailKitSimplified.Sender.Services
{
    public class Email : IEmail
    {
        [Required]
        public EmailContact From { get; set; }
        public IList<EmailContact> To { get; set; } = new List<EmailContact>();
        public IList<string> AttachmentFilePaths { get; set; } = new List<string>();
        public IEnumerable<string> AttachmentFileNames =>
            AttachmentFilePaths?.Select(a => Path.GetFileName(a)) ?? Array.Empty<string>();
        public int AttachmentCount => AttachmentFilePaths?.Count ?? 0;
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public bool IsHtml { get; set; } = false;

        private readonly IEmailSender _sender;

        public Email(IEmailSender emailSender)
        {
            _sender = emailSender ?? throw new ArgumentNullException(nameof(emailSender));
        }

        public IEmail Write(string fromAddress, string toAddress, string subject = "", string body = "", bool isHtml = true, params string[] attachmentFilePaths)
        {
            var converter = new MimeEntityConverter();
            From = converter.ParseEmailContacts(fromAddress).FirstOrDefault();
            To = converter.ParseEmailContacts(toAddress).ToList();
            Subject = subject ?? string.Empty;
            Body = body ?? string.Empty;
            IsHtml = isHtml;
            AttachmentFilePaths = attachmentFilePaths.ToList();
            return this;
        }

        public async Task<bool> SendAsync(CancellationToken token = default) =>
            await _sender.SendAsync(this, token);

        public override string ToString()
        {
            string envelope = string.Empty;
            using (var text = new StringWriter())
            {
                text.WriteLine("From: {0}", From);
                text.WriteLine("To: {0}", To.ToEnumeratedString(";"));
                if (AttachmentCount > 0)
                    text.WriteLine("{0} Attachment{1}: {2}",
                        AttachmentCount,
                        AttachmentCount == 1 ? "" : "s",
                        AttachmentFileNames.ToEnumeratedString(";"));
                text.WriteLine("Subject: {0}", Subject);
                envelope = text.ToString();
            }
            return envelope;
        }
    }
}
