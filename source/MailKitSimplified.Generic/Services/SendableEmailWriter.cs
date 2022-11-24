using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MailKitSimplified.Core.Abstractions;
using MailKitSimplified.Core.Models;

namespace MailKitSimplified.Core.Services
{
    public class SendableEmailWriter : ISendableEmailWriter
    {
        ISendableEmail ISendableEmailWriter.Result => _email;

        private readonly ISendableEmail _email;

        public SendableEmailWriter(ISendableEmail email) => _email = email;

        public static SendableEmailWriter CreateWith(ISmtpSender emailSender) => new SendableEmailWriter(new SendableEmail(emailSender));

        public ISendableEmailWriter Header(string key, string value)
        {
            _email.Headers.Add(key, value);
            return this;
        }

        public ISendableEmailWriter From(string emailAddress, string name = "")
        {
            _email.From.Add(EmailContact.Create(emailAddress, name));
            return this;
        }

        public ISendableEmailWriter ReplyTo(string emailAddress, string name = "")
        {
            _email.ReplyTo.Add(EmailContact.Create(emailAddress, name));
            return this;
        }

        public ISendableEmailWriter To(string emailAddress, string name = "")
        {
            _email.To.Add(EmailContact.Create(emailAddress, name));
            return this;
        }

        public ISendableEmailWriter Cc(string emailAddress, string name = "")
        {
            _email.Cc.Add(EmailContact.Create(emailAddress, name));
            return this;
        }

        public ISendableEmailWriter Bcc(string emailAddress, string name = "")
        {
            _email.Bcc.Add(EmailContact.Create(emailAddress, name));
            return this;
        }

        public ISendableEmailWriter Subject(string subject)
        {
            _email.Subject = subject ?? string.Empty;
            return this;
        }

        public ISendableEmailWriter Subject(string prefix, string suffix)
        {
            _email.Subject = $"{prefix}{_email.Subject}{suffix}";
            return this;
        }

        public ISendableEmailWriter Attach(string filePath)
        {
            if (!string.IsNullOrWhiteSpace(filePath))
            {
                _email.AttachmentFilePaths.Add(filePath);
            }
            return this;
        }

        public ISendableEmailWriter Attach(Stream stream, string contentId)
        {
            _email.Attachments.Add(contentId, stream);
            return this;
        }

        public ISendableEmailWriter BodyText(string plainText)
        {
            _email.BodyText = plainText ?? string.Empty;
            return this;
        }

        public ISendableEmailWriter BodyHtml(string htmlText)
        {
            _email.BodyText = htmlText ?? string.Empty;
            return this;
        }

        public ISendableEmailWriter Copy()
        {
            var copy = MemberwiseClone() as ISendableEmailWriter;
            return copy;
        }

        public async Task SendAsync(CancellationToken cancellationToken = default) =>
            await _email.SendAsync(cancellationToken).ConfigureAwait(false);

        public async Task<bool> TrySendAsync(CancellationToken cancellationToken = default) =>
            await _email.TrySendAsync(cancellationToken).ConfigureAwait(false);
    }
}
