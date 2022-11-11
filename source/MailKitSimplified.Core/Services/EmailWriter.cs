using System;
using System.Threading;
using System.Threading.Tasks;
using MailKitSimplified.Core.Abstractions;
using MailKitSimplified.Core.Models;

namespace MailKitSimplified.Core.Services
{
    public class EmailWriter : IEmailWriter
    {
        public ISendableEmail GetEmail => _email;

        private readonly ISendableEmail _email;

        public EmailWriter(ISendableEmail email)
        {
            _email = email;
        }

        public static EmailWriter CreateWith(ISendableEmail email) => new EmailWriter(email);

        public static EmailWriter CreateWith(IEmailSender emailSender) => CreateWith(new Email(emailSender));

        [Obsolete("Use 'CreateWith' instead.")]
        public static EmailWriter CreateFrom(ISendableEmail email) => new EmailWriter(email);

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

        public IEmailWriter Attach(params string[] filePaths)
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
