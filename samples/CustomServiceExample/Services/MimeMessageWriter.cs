using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using MimeKit;
using MimeKit.Text;
using MailKitSimplified.Core.Abstractions;
using MailKitSimplified.Core.Models;
using MailKitSimplified.Core.Services;
using MailKitSimplified.Sender.Abstractions;

namespace CustomServiceExample.Services
{
    [ExcludeFromCodeCoverage]
    public class MimeMessageWriter : IEmailWriter
    {
        public IEmail GetEmail
        {
            get
            {
                var from = _mimeMessage.From.Mailboxes;
                var to = _mimeMessage.To.Mailboxes;
                var email = new Email(_emailClient)
                {
                    From = from.Select(m => new EmailContact(m.Address, m.Name)).ToList(),
                    To = to.Select(m => new EmailContact(m.Address, m.Name)).ToList(),
                    Subject = _mimeMessage.Subject ?? string.Empty,
                    Body = _mimeMessage.HtmlBody ?? _mimeMessage.TextBody ?? string.Empty,
                };
                return email;
            }
        }

        private readonly MimeMessage _mimeMessage = new MimeMessage();
        private readonly IList<string> _attachmentFilePaths = new List<string>();
        private readonly IMimeMessageSender _emailClient;

        private MimeMessageWriter(IMimeMessageSender emailClient)
        {
            _emailClient = emailClient ?? throw new ArgumentNullException(nameof(emailClient));
        }

        public static MimeMessageWriter CreateWith(IMimeMessageSender emailClient) => new MimeMessageWriter(emailClient);

        public IEmailWriter From(string address, string name = "")
        {
            _mimeMessage.From.Add(new MailboxAddress(name, address));
            return this;
        }

        public IEmailWriter To(string address, string name = "")
        {
            _mimeMessage.To.Add(new MailboxAddress(name, address));
            return this;
        }

        public IEmailWriter Subject(string subject)
        {
            _mimeMessage.Subject = subject ?? string.Empty;
            return this;
        }

        public IEmailWriter Body(string bodyText, bool isHtml = true)
        {
            if (_mimeMessage.Body == null)
            {
                var format = isHtml ? TextFormat.Html : TextFormat.Plain;
                _mimeMessage.Body = new TextPart(format) { Text = bodyText ?? "" };
            }
            else
            {
                var builder = new BodyBuilder();
                if (isHtml)
                    builder.HtmlBody = bodyText;
                else if (_mimeMessage.HtmlBody != null)
                    builder.HtmlBody = _mimeMessage.HtmlBody;
                if (!isHtml)
                    builder.TextBody = bodyText;
                else if (_mimeMessage.TextBody != null)
                    builder.TextBody = _mimeMessage.TextBody;
                if (_mimeMessage.Attachments != null)
                {
                    var linkedResources = _mimeMessage.Attachments
                        .Where(attachment => !attachment.IsAttachment);
                    foreach (var linkedResource in linkedResources)
                        builder.LinkedResources.Add(linkedResource);
                    var attachments = _mimeMessage.Attachments
                        .Where(attachment => attachment.IsAttachment);
                    foreach (var attachment in attachments)
                        builder.Attachments.Add(attachment);
                }
                _mimeMessage.Body = builder.ToMessageBody();
            }
            return this;
        }

        public IEmailWriter Attach(params string[] filePaths)
        {
            if (filePaths != null)
            {
                foreach (var filePath in filePaths)
                {
                    _attachmentFilePaths.Add(filePath);
                }
            }
            return this;
        }

        public async Task SendAsync(CancellationToken cancellationToken = default)
        {
            await _emailClient.SendAsync(_mimeMessage, _attachmentFilePaths, cancellationToken).ConfigureAwait(false);
        }

        public async Task<bool> TrySendAsync(CancellationToken cancellationToken = default) =>
            await _emailClient.TrySendAsync(_mimeMessage, _attachmentFilePaths, cancellationToken).ConfigureAwait(false);
    }
}
