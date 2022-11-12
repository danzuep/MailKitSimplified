using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging;
using MailKitSimplified.Core.Abstractions;

namespace MailKitSimplified.Core.Services
{
    public class SendableEmail : BasicEmail, ISendableEmail
    {
        public IList<string> AttachmentFilePaths { get; set; } = new List<string>();

        public IEnumerable<string> AttachmentFileNames =>
            AttachmentFilePaths?.Select(a => Path.GetFileName(a)) ?? Array.Empty<string>();

        public int AttachmentCount => AttachmentFilePaths?.Count ?? 0;

        private readonly ISmtpSender _sender;
        private readonly ILogger _logger;

        public SendableEmail(ISmtpSender emailSender, ILogger<SendableEmail> logger = null)
        {
            _sender = emailSender ?? throw new ArgumentNullException(nameof(emailSender));
            _logger = logger ?? NullLogger<SendableEmail>.Instance;
        }

        public static SendableEmailWriter Create(ISmtpSender emailSender) => SendableEmailWriter.CreateWith(emailSender);

        public async Task SendAsync(CancellationToken cancellationToken = default)
        {
            await _sender.SendAsync(this, cancellationToken).ConfigureAwait(false);
            From.Clear();
            To.Clear();
            Cc.Clear();
            Bcc.Clear();
            AttachmentFilePaths.Clear();
            Subject = string.Empty;
            Body = string.Empty;
            IsHtml = false;
        }

        public async Task<bool> TrySendAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                await SendAsync(cancellationToken).ConfigureAwait(false);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email.");
                return false;
            }
        }

        public override string ToString()
        {
            string envelope = string.Empty;
            using (var text = new StringWriter())
            {
                text.WriteLine("From: {0}", string.Join(";", From));
                if (To.Count > 0)
                    text.WriteLine("To: {0}", string.Join(";", To));
                if (Cc.Count > 0)
                    text.WriteLine("Cc: {0}", string.Join(";", Cc));
                if (Bcc.Count > 0)
                    text.WriteLine("Bcc: {0}", string.Join(";", Bcc));
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
