using System.Threading.Tasks;
using System.Threading;
using MailKitSimplified.Core.Models;
using MailKitSimplified.Core.Abstractions;

namespace MailKitSimplified.Sender.Services
{
    public class EmailWriter : IEmailWriter
    {
        private IEmail _email;

        public EmailWriter(IEmailSender sender)
        {
            _email = new Email(sender);
        }

        public IEmailWriter From(string emailAddress, string name = "")
        {
            _email.From = new EmailContact(emailAddress, name);
            return this;
        }

        public IEmailWriter To(string emailAddress, string name = "")
        {
            _email.To.Add(new EmailContact(emailAddress, name));
            return this;
        }

        public IEmailWriter Subject(string subject)
        {
            _email.Subject = subject ?? string.Empty;
            return this;
        }

        public IEmailWriter Body(string body, bool isHtml)
        {
            _email.Body = body ?? string.Empty;
            _email.IsHtml = isHtml;
            return this;
        }

        public IEmailWriter Attach(params string[] filePaths)
        {
            if (filePaths != null)
                foreach (var filePath in filePaths)
                    if (!string.IsNullOrWhiteSpace(filePath))
                        _email.AttachmentFilePaths.Add(filePath);
            return this;
        }

        public async Task SendAsync(CancellationToken cancellationToken = default) =>
            await _email.SendAsync(cancellationToken).ConfigureAwait(false);
    }
}
