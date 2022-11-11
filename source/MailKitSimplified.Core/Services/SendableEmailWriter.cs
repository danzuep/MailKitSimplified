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

        public ISendableEmailWriter From(string emailAddress, string name = "")
        {
            _email.From.Add(EmailContact.Create(emailAddress, name));
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

        public ISendableEmailWriter Body(string body, bool isHtml = true)
        {
            _email.Body = body ?? string.Empty;
            _email.IsHtml = isHtml;
            return this;
        }

        public ISendableEmailWriter Attach(params string[] filePaths)
        {
            if (filePaths != null)
            {
                foreach (var filePath in filePaths)
                {
                    if (!string.IsNullOrWhiteSpace(filePath))
                    {
                        _email.AttachmentFilePaths.Add(filePath);
                    }
                }
            }
            return this;
        }

        public async Task SendAsync(CancellationToken cancellationToken = default) =>
            await _email.SendAsync(cancellationToken).ConfigureAwait(false);

        public async Task<bool> TrySendAsync(CancellationToken cancellationToken = default) =>
            await _email.TrySendAsync(cancellationToken).ConfigureAwait(false);
    }
}
