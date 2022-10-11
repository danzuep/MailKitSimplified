using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging;
using MailKitSimplified.Core.Abstractions;
using MailKitSimplified.Core.Models;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace MailKitSimplified.Core.Services
{
    public class Email : IEmail
    {
        [Required]
        public IList<EmailContact> From { get; set; } = new List<EmailContact>();
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

        public static EmailWriter Create(IEmailSender emailSender) => EmailWriter.CreateFrom(new Email(emailSender));

        [ExcludeFromCodeCoverage]
        [Obsolete("This method will be removed in a future version, use the Write method instead.")]
        public IEmail HandWrite(string fromAddress, string toAddress, string subject = "", string body = "", bool isHtml = true, params string[] attachmentFilePaths)
        {
            From = EmailContact.ParseEmailContacts(fromAddress).ToList();
            To = EmailContact.ParseEmailContacts(toAddress).ToList();
            Subject = subject ?? string.Empty;
            Body = body ?? string.Empty;
            IsHtml = isHtml;
            AttachmentFilePaths = attachmentFilePaths.ToList();
            return this;
        }

        public static void ValidateEmailAddresses(IEnumerable<string> fromEmailAddresses, IEnumerable<string> toEmailAddresses, ILogger logger)
        {
            if (fromEmailAddresses is null)
                throw new ArgumentNullException(nameof(fromEmailAddresses));
            if (toEmailAddresses is null)
                throw new ArgumentNullException(nameof(toEmailAddresses));
            if (logger is null)
                throw new ArgumentNullException(nameof(logger));
            foreach (var from in fromEmailAddresses)
            {
                if (!from.Contains("@"))
                    logger.LogWarning($"From address is invalid ({from})");
                foreach (var to in toEmailAddresses)
                {
                    if (!to.Contains("@"))
                        logger.LogWarning($"To address is invalid ({to})");
                    if (to.Equals(from, StringComparison.OrdinalIgnoreCase))
                        logger.LogWarning($"Circular reference, To ({to}) == From ({from})");
                }
            }
        }

        private void Validate()
        {
            if (!From.Any())
                throw new MissingMemberException(nameof(IEmail), nameof(IEmail.From));
            if (!To.Any())
                throw new MissingMemberException(nameof(IEmail), nameof(IEmail.To));
            var from = From.Select(m => m.Address);
            var to = To.Select(m => m.Address);
            ValidateEmailAddresses(from, to, _logger);
        }

        public async Task SendAsync(CancellationToken cancellationToken = default)
        {
            Validate();
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

        [ExcludeFromCodeCoverage]
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
