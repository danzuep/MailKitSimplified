using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using MimeKit;
using MimeKit.Text;
using MailKitSimplified.Sender.Abstractions;
using MailKitSimplified.Sender.Helpers;

namespace MailKitSimplified.Sender.Services
{
    public class EmailWriter : IEmailWriter
    {
        public MimeMessage MimeMessage => _mimeMessage;
        private readonly MimeMessage _mimeMessage = new MimeMessage();
        private readonly ISmtpSender _emailClient;

        public EmailWriter(ISmtpSender emailClient)
        {
            _emailClient = emailClient ?? throw new ArgumentNullException(nameof(emailClient));
        }

        public IEmailWriter From(string address, string name = "")
        {
            var fromMailboxAddress = new MailboxAddress(name, address);
            _mimeMessage.From.Add(fromMailboxAddress);
            _mimeMessage.ReplyTo.Add(fromMailboxAddress);
            return this;
        }

        public IEmailWriter From(string addresses)
        {
            var mailboxAddresses = MailboxAddressHelper.ParseEmailContacts(addresses);
            _mimeMessage.From.AddRange(mailboxAddresses);
            _mimeMessage.ReplyTo.AddRange(mailboxAddresses);
            return this;
        }

        public IEmailWriter To(string address, string name = "")
        {
            _mimeMessage.To.Add(new MailboxAddress(name, address));
            return this;
        }

        public IEmailWriter To(string addresses)
        {
            var mailboxAddresses = MailboxAddressHelper.ParseEmailContacts(addresses);
            _mimeMessage.To.AddRange(mailboxAddresses);
            return this;
        }

        public IEmailWriter Cc(string address, string name = "")
        {
            _mimeMessage.Cc.Add(new MailboxAddress(name, address));
            return this;
        }

        public IEmailWriter Cc(string addresses)
        {
            var mailboxAddresses = MailboxAddressHelper.ParseEmailContacts(addresses);
            _mimeMessage.Cc.AddRange(mailboxAddresses);
            return this;
        }

        public IEmailWriter Bcc(string address, string name = "")
        {
            _mimeMessage.Bcc.Add(new MailboxAddress(name, address));
            return this;
        }

        public IEmailWriter Bcc(string addresses)
        {
            var mailboxAddresses = MailboxAddressHelper.ParseEmailContacts(addresses);
            _mimeMessage.Bcc.AddRange(mailboxAddresses);
            return this;
        }

        public IEmailWriter Subject(string subject, bool append = false)
        {
            if (_mimeMessage.Subject == null)
                _mimeMessage.Subject = subject ?? string.Empty;
            else if (append)
                _mimeMessage.Subject = $"{_mimeMessage.Subject}{subject}";
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
                var builder = new BodyBuilder();
                builder.HtmlBody = _mimeMessage.HtmlBody;
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
                foreach (var filePath in filePaths)
                    builder.Attachments.Add(filePath);
                _mimeMessage.Body = builder.ToMessageBody();
            }
            return this;
        }

        public IEmailWriter Attach(MimePart mimePart, bool resource = false)
        {
            if (_mimeMessage.Body == null)
            {
                _mimeMessage.Body = mimePart;
            }
            else if (mimePart != null)
            {
                var builder = new BodyBuilder();
                builder.HtmlBody = _mimeMessage.HtmlBody;
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
                if (!resource)
                    builder.Attachments.Add(mimePart);
                else
                    builder.LinkedResources.Add(mimePart);
                _mimeMessage.Body = builder.ToMessageBody();
            }
            return this;
        }

        public IEmailWriter Attach(IEnumerable<MimePart> mimeParts, bool resource = false)
        {
            if (mimeParts != null && mimeParts.Any())
            {
                var builder = new BodyBuilder();
                builder.HtmlBody = _mimeMessage.HtmlBody;
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
                foreach(var mimePart in mimeParts)
                    if (!resource)
                        builder.Attachments.Add(mimePart);
                    else
                        builder.LinkedResources.Add(mimePart);
                _mimeMessage.Body = builder.ToMessageBody();
            }
            return this;
        }

        public async Task SendAsync(CancellationToken cancellationToken = default)
        {
            await _emailClient.SendAsync(_mimeMessage, cancellationToken).ConfigureAwait(false);
        }

        public async Task<bool> TrySendAsync(CancellationToken cancellationToken = default) =>

            await _emailClient.TrySendAsync(_mimeMessage, cancellationToken).ConfigureAwait(false);
    }
}
