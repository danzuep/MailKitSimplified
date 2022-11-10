using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging;
using MailKitSimplified.Core.Abstractions;
using MailKitSimplified.Core.Models;

namespace MailKitSimplified.Core.Services
{
    public class Email : ISendableEmail
    {
        [Required]
        public IList<IEmailAddress> From { get; set; } = new List<IEmailAddress>();
        public IList<IEmailAddress> To { get; set; } = new List<IEmailAddress>();
        public IList<IEmailAddress> Cc { get; set; } = new List<IEmailAddress>();
        public IList<IEmailAddress> Bcc { get; set; } = new List<IEmailAddress>();
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

        public static EmailWriter Create(IEmailSender emailSender) => EmailWriter.CreateWith(emailSender);

        [ExcludeFromCodeCoverage]
        [Obsolete("This method will be removed in a future version, use the Write method instead.")]
        public ISendableEmail HandWrite(string fromAddress, string toAddress, string subject = "", string body = "", bool isHtml = true, params string[] attachmentFilePaths)
        {
            From = EmailContact.ParseEmailContacts(fromAddress).ToList();
            To = EmailContact.ParseEmailContacts(toAddress).ToList();
            Subject = subject ?? string.Empty;
            Body = body ?? string.Empty;
            IsHtml = isHtml;
            AttachmentFilePaths = attachmentFilePaths.ToList();
            return this;
        }

        public async Task SendAsync(CancellationToken cancellationToken = default)
        {
            await _sender.SendAsync(this, cancellationToken).ConfigureAwait(false);
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
                text.WriteLine("From: {0}", string.Join(";", From));
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
