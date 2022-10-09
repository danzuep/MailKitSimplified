using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using MailKitSimplified.Core.Abstractions;
using MailKitSimplified.Core.Models;
using MailKitSimplified.Sender.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging;

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
        private readonly ILogger _logger;

        public Email(IEmailSender emailSender, ILogger<Email> logger = null)
        {
            _sender = emailSender ?? throw new ArgumentNullException(nameof(emailSender));
            _logger = logger ?? NullLogger<Email>.Instance;
        }

        public static IEmailWriter Write(IEmailSender emailSender) => EmailWriter.CreateFrom(new Email(emailSender));

        [Obsolete("This method will be removed in a future version, use the Write method instead.")]
        public IEmail HandWrite(string fromAddress, string toAddress, string subject = "", string body = "", bool isHtml = true, params string[] attachmentFilePaths)
        {
            From = EmailContact.ParseEmailContacts(fromAddress).FirstOrDefault();
            To = EmailContact.ParseEmailContacts(toAddress).ToList();
            Subject = subject ?? string.Empty;
            Body = body ?? string.Empty;
            IsHtml = isHtml;
            AttachmentFilePaths = attachmentFilePaths.ToList();
            return this;
        }

        private bool HasCircularReference => To.Any(t => t.Address.Equals(From.Address, StringComparison.OrdinalIgnoreCase));

        private void Validate()
        {
            if (To.Count == 0)
                throw new MissingMemberException(nameof(IEmail), nameof(IEmail.To));
            if (HasCircularReference)
                _logger.LogWarning("Circular reference, ToEmailAddress == FromEmailAddress");
            if (!From.Address.Contains("@"))
                _logger.LogWarning($"From address is invalid ({From})");
            foreach (var to in To)
                if (!to.Address.Contains("@"))
                    _logger.LogWarning($"To address is invalid ({to})");
        }

        public async Task SendAsync(CancellationToken token = default)
        {
            Validate();
            await _sender.SendAsync(this, token).ConfigureAwait(false);
        }


        public async Task<bool> TrySendAsync(CancellationToken cancellationToken = default)
        {
            bool isSent = false;
            try
            {
                await SendAsync(cancellationToken).ConfigureAwait(false);
                isSent = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email.");
            }
            return isSent;
        }

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
