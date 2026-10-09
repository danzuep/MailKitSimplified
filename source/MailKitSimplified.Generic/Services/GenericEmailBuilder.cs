using System.Collections.Generic;
using System.Linq;
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

        /// <summary>
        /// Builds a detached snapshot without performing I/O.
        /// </summary>
        /// <remarks>
        /// Contacts, collections, and byte arrays are copied. Other attachment
        /// values, including streams, remain caller-owned references.
        /// </remarks>
        /// <returns>A mutable email independent of subsequent builder changes.</returns>
        public GenericEmail Build() => new GenericEmail
        {
            Headers = new Dictionary<string, string>(_email.Headers),
            From = _email.From.Select(CopyContact).ToList(),
            ReplyTo = _email.ReplyTo.Select(CopyContact).ToList(),
            To = _email.To.Select(CopyContact).ToList(),
            Cc = _email.Cc.Select(CopyContact).ToList(),
            Bcc = _email.Bcc.Select(CopyContact).ToList(),
            Attachments = _email.Attachments.ToDictionary(
                attachment => attachment.Key,
                attachment => attachment.Value is byte[] bytes ? bytes.Clone() : attachment.Value),
            Subject = _email.Subject,
            BodyText = _email.BodyText,
            BodyHtml = _email.BodyHtml
        };

        /// <summary>
        /// Copies the builder using the same attachment ownership rules as <see cref="Build"/>.
        /// </summary>
        /// <returns>A builder with independent email state.</returns>
        public GenericEmailBuilder Copy() => new GenericEmailBuilder
        {
            _email = Build(),
            _defaultFrom = _defaultFrom == null ? null : CopyContact(_defaultFrom)
        };

        private static IGenericEmailContact CopyContact(IGenericEmailContact contact)
        {
            var copy = GenericEmailContact.Create(contact.EmailAddress, contact.Name);
            return copy;
        }
    }
}
