using System.Net.Mail;
using System.Xml.Linq;

namespace EmailWpfApp.Models
{
    public class EmailWriter
    {
        public Email AsEmail => _email;

        private readonly Email _email;

        public EmailWriter(Email? email = null)
        {
            _email = email ?? new Email();
        }

        public EmailWriter From(string emailAddress, string name = "")
        {
            var emailContact = $"\"{name}\" <{emailAddress}>";
            _email.From = string.IsNullOrEmpty(_email.From) ?
                emailContact : $"{_email.From}; {emailContact}";
            return this;
        }

        public EmailWriter To(string emailAddress, string name = "")
        {
            var emailContact = $"\"{name}\" <{emailAddress}>";
            _email.To = string.IsNullOrEmpty(_email.To) ?
                emailContact : $"{_email.To}; {emailContact}";
            return this;
        }

        public EmailWriter Cc(string emailAddress, string name = "")
        {
            var emailContact = $"\"{name}\" <{emailAddress}>";
            _email.Cc = string.IsNullOrEmpty(_email.Cc) ?
                emailContact : $"{_email.Cc}; {emailContact}";
            return this;
        }

        public EmailWriter Bcc(string emailAddress, string name = "")
        {
            var emailContact = $"\"{name}\" <{emailAddress}>";
            _email.Bcc = string.IsNullOrEmpty(_email.Bcc) ?
                emailContact : $"{_email.Bcc}; {emailContact}";
            return this;
        }

        public EmailWriter Subject(string subject)
        {
            _email.Subject = subject ?? string.Empty;
            return this;
        }

        public EmailWriter Subject(string prefix, string suffix)
        {
            _email.Subject = $"{prefix}{_email.Subject}{suffix}";
            return this;
        }

        public EmailWriter Header(string key, string value)
        {
            var header = $"\"{key}\": \"{value}\"";
            _email.Headers = string.IsNullOrEmpty(_email.Headers) ?
                header : $"{_email.Headers}; {header}";
            return this;
        }

        public EmailWriter Attach(string key, object? value = null)
        {
            var attachment = $"\"{key}\": \"{value}\"";
            _email.Attachments = string.IsNullOrEmpty(_email.Attachments) ?
                attachment : $"{_email.Attachments}; {attachment}";
            return this;
        }

        public EmailWriter BodyText(string plainText)
        {
            _email.BodyText = plainText ?? string.Empty;
            return this;
        }

        public EmailWriter BodyHtml(string htmlText)
        {
            _email.BodyHtml = htmlText ?? string.Empty;
            return this;
        }

        public EmailWriter Copy()
        {
            var email = this.AsEmail.Copy();
            var writer = new EmailWriter(email);
            return writer;
        }
    }
}
