using System.Threading.Tasks;
using System.Threading;
using MailKitSimplified.Sender.Models;
using MailKitSimplified.Sender.Abstractions;

namespace MailKitSimplified.Sender.Services
{
    public class FluentEmail : IFluentEmail
    {
        private IEmail _email;

        public FluentEmail(IEmailSender sender)
        {
            _email = new Email(sender);
        }

        public IFluentEmail From(string emailAddress, string name = "")
        {
            _email.From = new EmailContact(emailAddress, name);
            return this;
        }

        public IFluentEmail To(string emailAddress, string name = "")
        {
            _email.To.Add(new EmailContact(emailAddress, name));
            return this;
        }

        public IFluentEmail Subject(string subject)
        {
            _email.Subject = subject ?? string.Empty;
            return this;
        }

        public IFluentEmail Body(string body, bool isHtml)
        {
            _email.Body = body ?? string.Empty;
            _email.IsHtml = isHtml;
            return this;
        }

        public IFluentEmail Attach(params string[] filePaths)
        {
            if (filePaths != null)
                foreach (var filePath in filePaths)
                    if (!string.IsNullOrWhiteSpace(filePath))
                        _email.AttachmentFilePaths.Add(filePath);
            return this;
        }

        public async Task SendAsync(CancellationToken cancellationToken = default) =>
            await _email.SendAsync(cancellationToken);
    }
}
