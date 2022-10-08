using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using MimeKit;
using MimeKit.Text;
using MailKitSimplified.Core.Abstractions;
using MailKitSimplified.Sender.Abstractions;

namespace MailKitSimplified.Sender.Services
{
    public class MimeEmailWriter : IEmailWriter
    {
        private MimeMessage _mimeMessage = new MimeMessage();
        private IList<string> _attachmentFilePaths = new List<string>();

        private readonly IMimeEmailSender _emailClient;

        public MimeEmailWriter(IMimeEmailSender emailClient)
        {
            _emailClient = emailClient ?? throw new ArgumentNullException(nameof(emailClient));
        }

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
    }
}
