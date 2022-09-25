using System.Threading.Tasks;
using System.Threading;
using MailKitSimplified.Sender.Models;
using MailKitSimplified.Sender.Abstractions;
using System.Linq;
using System;

namespace MailKitSimplified.Sender.Services
{
    // inspired by https://github.com/lukencode/FluentEmail/blob/master/src/FluentEmail.Core/Email.cs
    public class FluentEmail : IFluentEmail
    {
        private IEmail _email;

        public FluentEmail(IEmailSender sender)
        {
            _email = new Email(sender);
        }

        public IFluentEmail From(string emailAddress, string name = "")
        {
            var contact = new EmailContact(emailAddress, name);
            _email.From = MimeEntityConverter.FormatContactName(contact);
            return this;
        }

        IFluentEmail IFluentEmail.To(string emailAddress, string name)
        {
            var contact = new EmailContact(emailAddress, name);
            _email.To.Add(MimeEntityConverter.FormatContactName(contact));
            return this;
        }

        IFluentEmail IFluentEmail.Subject(string subject)
        {
            _email.Subject = subject ?? string.Empty;
            return this;
        }

        IFluentEmail IFluentEmail.Body(string body, bool isHtml)
        {
            _email.Body = body ?? string.Empty;
            _email.IsHtml = isHtml;
            return this;
        }

        IFluentEmail IFluentEmail.Attach(params string[] filePaths)
        {
            if (filePaths != null)
                foreach (var filePath in filePaths)
                    _email.AttachmentFilePaths.Add(filePath);
            return this;
        }

        public async Task SendAsync(CancellationToken cancellationToken = default) =>
            await _email.SendAsync(cancellationToken);
    }
}
