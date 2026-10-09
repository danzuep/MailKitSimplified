using MailKitSimplified.Generic.Abstractions;
using MailKitSimplified.Generic.Models;

namespace MailKitSimplified.Generic.Services
{
    public class GenericEmailBuilder
    {
        public GenericEmail AsEmail => _email;

        private GenericEmail _email = new GenericEmail();

        private IGenericEmailContact _defaultFrom = null;

        public GenericEmailBuilder Header(string key, string value)
        {
            _email.Headers.Add(key, value);
            return this;
        }

        public GenericEmailBuilder DefaultFrom(string emailAddress, string name = null)
        {
            _defaultFrom = GenericEmailContact.Create(emailAddress, name);
            _email.From.Add(_defaultFrom);
            return this;
        }

        public GenericEmailBuilder From(string emailAddress, string name = null)
        {
            _email.From.Add(GenericEmailContact.Create(emailAddress, name));
            return this;
        }

        public GenericEmailBuilder To(string emailAddress, string name = null)
        {
            _email.To.Add(GenericEmailContact.Create(emailAddress, name));
            return this;
        }

        public GenericEmailBuilder Cc(string emailAddress, string name = null)
        {
            _email.Cc.Add(GenericEmailContact.Create(emailAddress, name));
            return this;
        }

        public GenericEmailBuilder Bcc(string emailAddress, string name = null)
        {
            _email.Bcc.Add(GenericEmailContact.Create(emailAddress, name));
            return this;
        }

        public GenericEmailBuilder Subject(string subject)
        {
            _email.Subject = subject ?? string.Empty;
            return this;
        }

        public GenericEmailBuilder Subject(string prefix, string suffix)
        {
            _email.Subject = $"{prefix}{_email.Subject}{suffix}";
            return this;
        }

        public GenericEmailBuilder Attach(string key, object value = null)
        {
            _email.Attachments.Add(key, value);
            return this;
        }

        public GenericEmailBuilder BodyText(string plainText)
        {
            _email.BodyText = plainText ?? string.Empty;
            return this;
        }

        public GenericEmailBuilder BodyHtml(string htmlText)
        {
            _email.BodyHtml = htmlText ?? string.Empty;
            return this;
        }

        public GenericEmailBuilder Copy() => MemberwiseClone() as GenericEmailBuilder;
    }
}
