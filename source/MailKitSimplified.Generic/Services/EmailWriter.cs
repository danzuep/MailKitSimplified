using MailKitSimplified.Core.Abstractions;
using MailKitSimplified.Core.Models;

namespace MailKitSimplified.Core.Services
{
    public class EmailWriter : IEmailWriter
    {
        public IBasicEmail Result => _email;

        private readonly IBasicEmail _email;

        public EmailWriter(IBasicEmail email = null)
        {
            _email = email ?? new BasicEmail();
        }

        public IEmailWriter From(string emailAddress, string name = "")
        {
            _email.From.Add(EmailContact.Create(emailAddress, name));
            return this;
        }

        public IEmailWriter To(string emailAddress, string name = "")
        {
            _email.To.Add(EmailContact.Create(emailAddress, name));
            return this;
        }

        public IEmailWriter Cc(string emailAddress, string name = "")
        {
            _email.Cc.Add(EmailContact.Create(emailAddress, name));
            return this;
        }

        public IEmailWriter Bcc(string emailAddress, string name = "")
        {
            _email.Bcc.Add(EmailContact.Create(emailAddress, name));
            return this;
        }

        public IEmailWriter Subject(string subject)
        {
            _email.Subject = subject ?? string.Empty;
            return this;
        }

        public IEmailWriter Body(string body, bool isHtml = true)
        {
            _email.Body = body ?? string.Empty;
            _email.IsHtml = isHtml;
            return this;
        }
    }
}
