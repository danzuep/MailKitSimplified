using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using MimeKit;
using MimeKit.Text;
using MailKitSimplified.Sender.Abstractions;
using MailKitSimplified.Sender.Helpers;
using System.IO;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MailKitSimplified.Sender.Extensions;

namespace MailKitSimplified.Sender.Services
{
    public class EmailWriter : IEmailWriter
    {
        public MimeMessage MimeMessage => _mimeMessage;
        private MimeMessage _mimeMessage = new MimeMessage();
        private readonly ILogger _logger;
        private readonly ISmtpSender _emailClient;

        public EmailWriter(ISmtpSender emailClient, ILogger<EmailWriter> logger = null)
        {
            _logger = logger ?? NullLogger<EmailWriter>.Instance;
            _emailClient = emailClient ?? throw new ArgumentNullException(nameof(emailClient));
        }

        public IEmailWriter From(string name, string address, bool replyTo = true)
        {
            var fromMailboxAddress = new MailboxAddress(name, address);
            _mimeMessage.From.Add(fromMailboxAddress);
            if (replyTo)
                _mimeMessage.ReplyTo.Add(fromMailboxAddress);
            return this;
        }

        public IEmailWriter From(string addresses, bool replyTo = true)
        {
            var mailboxAddresses = MailboxAddressHelper.ParseEmailContacts(addresses);
            _mimeMessage.From.AddRange(mailboxAddresses);
            if (replyTo)
                _mimeMessage.ReplyTo.AddRange(mailboxAddresses);
            return this;
        }

        public IEmailWriter To(string name, string address)
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

        public IEmailWriter Cc(string name, string address)
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

        public IEmailWriter Bcc(string name, string address)
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
            if (_mimeMessage.Subject == null || !append)
                _mimeMessage.Subject = subject ?? string.Empty;
            else
                _mimeMessage.Subject = $"{_mimeMessage.Subject}{subject}";
            return this;
        }

        public IEmailWriter BodyText(string textPlain) => Body(textPlain, false);

        public IEmailWriter BodyHtml(string textHtml) => Body(textHtml, true);

        private IEmailWriter Body(string bodyText, bool isHtml)
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
                var builder = new BodyBuilder
                {
                    HtmlBody = _mimeMessage.HtmlBody,
                    TextBody = _mimeMessage.TextBody
                };
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
                var builder = new BodyBuilder
                {
                    HtmlBody = _mimeMessage.HtmlBody,
                    TextBody = _mimeMessage.TextBody
                };
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
                var builder = new BodyBuilder
                {
                    HtmlBody = _mimeMessage.HtmlBody,
                    TextBody = _mimeMessage.TextBody
                };
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

        public IEmailWriter TryAttach(params string[] filePaths)
        {
            if (filePaths != null)
            {
                var builder = new BodyBuilder
                {
                    HtmlBody = _mimeMessage.HtmlBody,
                    TextBody = _mimeMessage.TextBody
                };
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
                {
                    try
                    {
                        builder.Attachments.Add(filePath);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Failed to load attachment: {filePath}");
                    }
                }
                _mimeMessage.Body = builder.ToMessageBody();
            }
            return this;
        }

        public void Send(CancellationToken cancellationToken = default) =>
            SendAsync(cancellationToken).ConfigureAwait(false).GetAwaiter().GetResult();

        public async Task SendAsync(CancellationToken cancellationToken = default)
        {
            await _emailClient.SendAsync(_mimeMessage, cancellationToken).ConfigureAwait(false);
            _mimeMessage = new MimeMessage();
        }

        public bool TrySend(CancellationToken cancellationToken = default) =>
            TrySendAsync(cancellationToken).ConfigureAwait(false).GetAwaiter().GetResult();

        public async Task<bool> TrySendAsync(CancellationToken cancellationToken = default)
        {
            bool isSent = await _emailClient.TrySendAsync(_mimeMessage, cancellationToken).ConfigureAwait(false);
            _mimeMessage = new MimeMessage();
            return isSent;
        }

        public override string ToString()
        {
            string envelope = string.Empty;
            using (var text = new StringWriter())
            {
                text.WriteLine("Date: {0}", _mimeMessage.Date);
                if (_mimeMessage.From.Count > 0)
                    text.WriteLine("From: {0}", string.Join(";", _mimeMessage.From.Mailboxes));
                if (_mimeMessage.To.Count > 0)
                    text.WriteLine("To: {0}", string.Join(";", _mimeMessage.To.Mailboxes));
                if (_mimeMessage.Cc.Count > 0)
                    text.WriteLine("Cc: {0}", string.Join(";", _mimeMessage.Cc.Mailboxes));
                if (_mimeMessage.Bcc.Count > 0)
                    text.WriteLine("Bcc: {0}", string.Join(";", _mimeMessage.Bcc.Mailboxes));
                text.WriteLine("Subject: {0}", _mimeMessage.Subject);
                text.WriteLine("Message-Id: <{0}>", _mimeMessage.MessageId);
                var attachmentCount = _mimeMessage.Attachments.Count();
                if (attachmentCount > 0)
                    text.WriteLine("{0} Attachment{1}: {2}",
                        attachmentCount, attachmentCount == 1 ? "" : "s",
                        string.Join(";", _mimeMessage.Attachments.GetAttachmentNames()));
                envelope = text.ToString();
            }
            return envelope;
        }
    }
}
