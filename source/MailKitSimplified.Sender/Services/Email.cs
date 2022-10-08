using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using MailKitSimplified.Core.Abstractions;
using MailKitSimplified.Core.Models;

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

        public IEmailWriter Fluent => new EmailWriter(_sender);

        public IEmail Write(string fromAddress, string toAddress, string subject = "", string body = "", bool isHtml = true, params string[] attachmentFilePaths)
        {
            From = EmailContact.ParseEmailContacts(fromAddress).FirstOrDefault();
            To = EmailContact.ParseEmailContacts(toAddress).ToList();
            Subject = subject ?? string.Empty;
            Body = body ?? string.Empty;
            IsHtml = isHtml;
            AttachmentFilePaths = attachmentFilePaths.ToList();
            return this;
        }

        public async Task SendAsync(CancellationToken token = default) =>
            await _sender.SendAsync(this, token);

        public override string ToString()
        {
            string envelope = string.Empty;
            using (var text = new StringWriter())
            {
                text.WriteLine("From: {0}", From);
                text.WriteLine("To: {0}", string.Join(";", To));
                if (AttachmentCount > 0)
                    text.WriteLine("{0} Attachment{1}: {2}",
                        AttachmentCount,
                        AttachmentCount == 1 ? "" : "s",
                        string.Join(";", AttachmentFileNames));
                text.WriteLine("Subject: {0}", Subject);
                envelope = text.ToString();
            }
            return envelope;
        }
    }
}
