using MailKitSimplified.Core.Abstractions;
using MailKitSimplified.Core.Models;

namespace MailKitSimplified.Core.Services
{
    public class EmailWriter : IBasicEmailWriter
    {
        public IBasicEmail Result => _email;

        private readonly IBasicEmail _email;

        public EmailWriter(IBasicEmail email = null)
        {
            _email = email ?? new BasicEmail();
        }

        public IBasicEmailWriter Header(string key, string value)
        {
            _email.Headers.Add(key, value);
            return this;
        }

        public IBasicEmailWriter From(string emailAddress, string name = "")
        {
            _email.From.Add(EmailContact.Create(emailAddress, name));
            return this;
        }

        public IBasicEmailWriter To(string emailAddress, string name = "")
        {
            _email.To.Add(EmailContact.Create(emailAddress, name));
            return this;
        }

        public IBasicEmailWriter Cc(string emailAddress, string name = "")
        {
            _email.Cc.Add(EmailContact.Create(emailAddress, name));
            return this;
        }

        public IBasicEmailWriter Bcc(string emailAddress, string name = "")
        {
            _email.Bcc.Add(EmailContact.Create(emailAddress, name));
            return this;
        }

        public IBasicEmailWriter Subject(string subject)
        {
            _email.Subject = subject ?? string.Empty;
            return this;
        }

        public IBasicEmailWriter Subject(string prefix, string suffix)
        {
            _email.Subject = $"{prefix}{_email.Subject}{suffix}";
            return this;
        }

        public IBasicEmailWriter Attach(string key, object value)
        {
            _email.Attachments.Add(key, value);
            return this;
        }

        public IBasicEmailWriter BodyText(string plainText)
        {
            _email.BodyText = plainText ?? string.Empty;
            return this;
        }

        public IBasicEmailWriter BodyHtml(string htmlText)
        {
            _email.BodyHtml = htmlText ?? string.Empty;
            return this;
        }

        public IBasicEmailWriter Copy()
        {
            var copy = MemberwiseClone() as IBasicEmailWriter;
            return copy;
        }
    }
}
