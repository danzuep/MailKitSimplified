using MimeKit;
using MimeKit.IO;
using MimeKit.Text;
using MimeKit.Utils;
using MailKit;
using System;
using System.Linq;
using System.IO;
using System.IO.Abstractions;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging;
using MailKitSimplified.Sender.Abstractions;
using MailKitSimplified.Sender.Helpers;
using MailKitSimplified.Sender.Extensions;

namespace MailKitSimplified.Sender.Services
{
    public class EmailWriter : IEmailWriter
    {
        public MimeMessage MimeMessage => _mimeMessage;
        private MimeMessage _mimeMessage = new MimeMessage();
        private MailboxAddress _defaultFrom;

        private readonly ILogger _logger;
        private readonly ISmtpSender _emailClient;
        private readonly IFileSystem _fileSystem;

        public EmailWriter(ISmtpSender emailClient, ILogger<EmailWriter> logger = null, IFileSystem fileSystem = null)
        {
            _logger = logger ?? NullLogger<EmailWriter>.Instance;
            _emailClient = emailClient ?? throw new ArgumentNullException(nameof(emailClient));
            _fileSystem = fileSystem ?? new FileSystem();
        }

        public IEmailWriter DefaultFrom(string name, string address)
        {
            _defaultFrom = new MailboxAddress(name, address);
            _mimeMessage.From.Add(_defaultFrom);
            return this;
        }

        public IEmailWriter From(string name, string address)
        {
            var fromMailboxAddress = new MailboxAddress(name, address);
            _mimeMessage.From.Add(fromMailboxAddress);
            return this;
        }

        public IEmailWriter From(string addresses)
        {
            var mailboxAddresses = MailboxAddressHelper.ParseEmailContacts(addresses);
            _mimeMessage.From.AddRange(mailboxAddresses);
            return this;
        }

        public IEmailWriter ReplyTo(string name, string address)
        {
            _mimeMessage.ReplyTo.Add(new MailboxAddress(name, address));
            return this;
        }

        public IEmailWriter ReplyTo(string addresses)
        {
            var mailboxAddresses = MailboxAddressHelper.ParseEmailContacts(addresses);
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

        public IEmailWriter Subject(string subject)
        {
            _mimeMessage.Subject = subject ?? string.Empty;
            return this;
        }

        public IEmailWriter Subject(string prefix, string suffix)
        {
            var original = _mimeMessage.Subject ?? string.Empty;
            _mimeMessage.Subject = $"{prefix}{original}{suffix}";
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
                var builder = BuildMessage(_mimeMessage);
                if (isHtml)
                    builder.HtmlBody = bodyText;
                else
                    builder.TextBody = bodyText;
                _mimeMessage.Body = builder.ToMessageBody();
            }
            return this;
        }

        public static BodyBuilder BuildMessage(MimeMessage mimeMessage)
        {
            var builder = new BodyBuilder
            {
                TextBody = mimeMessage.TextBody,
                HtmlBody = mimeMessage.HtmlBody
            };
            var linkedResources = mimeMessage.BodyParts
                .Where(part => !part.IsAttachment && part.ContentId != null &&
                    ((mimeMessage.HtmlBody?.Contains(part.ContentId) ?? false) ||
                    (mimeMessage.TextBody?.Contains(part.ContentId) ?? false)));
            foreach (var resource in linkedResources)
                builder.LinkedResources.Add(resource);
            foreach (var attachment in mimeMessage.Attachments)
                builder.Attachments.Add(attachment);
            return builder;
        }

        public static MimePart GetMimePart(Stream stream, string fileName, string contentType = null, string contentId = null)
        {
            stream.Position = 0; // reset stream position ready to read
            if (string.IsNullOrWhiteSpace(contentType))
                contentType = MimeTypes.GetMimeType(fileName);
            if (string.IsNullOrWhiteSpace(contentId))
                contentId = MimeUtils.GenerateMessageId();
            var attachment = ContentDisposition.Attachment;
            var mimePart = new MimePart(contentType)
            {
                Content = new MimeContent(stream),
                ContentTransferEncoding = ContentEncoding.Base64,
                ContentDisposition = new ContentDisposition(attachment),
                ContentId = contentId,
                FileName = fileName ?? string.Empty,
            };
            return mimePart;
        }

        public static MimePart GetMimePart(string filePath, IFileSystem fileSystem = null)
        {
            if (fileSystem == null)
                fileSystem = new FileSystem();
            var memoryStream = new MemoryBlockStream();
            using (var stream = fileSystem.File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                stream.CopyTo(memoryStream);
            string fileName = fileSystem.Path.GetFileName(filePath);
            var mimePart = GetMimePart(memoryStream, fileName);
            return mimePart;
        }

        public IEmailWriter Attach(params string[] filePaths)
        {
            if (filePaths != null && filePaths.Length > 0)
            {
                var mimeEntities = new List<MimePart>();
                foreach (var filePath in filePaths)
                {
                    var mimeEntity = GetMimePart(filePath, _fileSystem);
                    mimeEntities.Add(mimeEntity);
                }
                Attach(mimeEntities);
            }
            return this;
        }

        public IEmailWriter Attach(Stream stream, string fileName, string contentType = null, string contentId = null, bool linkedResource = false) =>
            Attach(GetMimePart(stream, fileName, contentType, contentId), linkedResource);

        public IEmailWriter Attach(MimeEntity mimeEntity, bool linkedResource = false)
        {
            if (_mimeMessage.Body == null)
            {
                _mimeMessage.Body = mimeEntity;
            }
            else if (mimeEntity != null)
            {
                var builder = BuildMessage(_mimeMessage);
                if (!linkedResource)
                    builder.Attachments.Add(mimeEntity);
                else
                    builder.LinkedResources.Add(mimeEntity);
                _mimeMessage.Body = builder.ToMessageBody();
            }
            return this;
        }

        public IEmailWriter Attach(IEnumerable<MimeEntity> mimeEntities, bool linkedResource = false)
        {
            if (mimeEntities != null && mimeEntities.Any())
            {
                var builder = BuildMessage(_mimeMessage);
                foreach (var mimePart in mimeEntities)
                    if (!linkedResource)
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
                var builder = BuildMessage(_mimeMessage);
                foreach (var filePath in filePaths)
                {
                    try
                    {
                        if (!string.IsNullOrWhiteSpace(filePath) &&
                            _fileSystem.File.Exists(filePath))
                            builder.Attachments.Add(filePath);
                        else
                            _logger.LogWarning($"Failed to load attachment: {filePath}");
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

        public IEmailWriter Header(string field, string value)
        {
            _mimeMessage.Headers.Add(field, value);
            return this;
        }

        public IEmailWriter Priority(MessagePriority priority)
        {
            _mimeMessage.Priority = priority;
            return this;
        }

        public void Send(CancellationToken cancellationToken = default) =>
            SendAsync(cancellationToken).ConfigureAwait(false).GetAwaiter().GetResult();

        public async Task SendAsync(CancellationToken cancellationToken = default, ITransferProgress transferProgress = null)
        {
            await _emailClient.SendAsync(_mimeMessage, cancellationToken, transferProgress).ConfigureAwait(false);
            _mimeMessage = new MimeMessage();
            if (_defaultFrom != null)
                _mimeMessage.From.Add(_defaultFrom);
        }

        public bool TrySend(CancellationToken cancellationToken = default) =>
            TrySendAsync(cancellationToken).ConfigureAwait(false).GetAwaiter().GetResult();

        public async Task<bool> TrySendAsync(CancellationToken cancellationToken = default, ITransferProgress transferProgress = null)
        {
            bool isSent = await _emailClient.TrySendAsync(_mimeMessage, cancellationToken, transferProgress).ConfigureAwait(false);
            _mimeMessage = new MimeMessage();
            if (_defaultFrom != null)
                _mimeMessage.From.Add(_defaultFrom);
            return isSent;
        }

        public IEmailWriter Copy() => MemberwiseClone() as IEmailWriter;

        public override string ToString()
        {
            string envelope = string.Empty;
            using (var text = new StringWriter())
            {
                text.WriteLine("Message-Id: <{0}>", _mimeMessage.MessageId);
                text.WriteLine("Date: {0}", _mimeMessage.Date);
                if (_mimeMessage.From.Count > 0)
                    text.WriteLine("From: {0}", string.Join("; ", _mimeMessage.From.Mailboxes));
                if (_mimeMessage.To.Count > 0)
                    text.WriteLine("To: {0}", string.Join("; ", _mimeMessage.To.Mailboxes));
                if (_mimeMessage.Cc.Count > 0)
                    text.WriteLine("Cc: {0}", string.Join("; ", _mimeMessage.Cc.Mailboxes));
                if (_mimeMessage.Bcc.Count > 0)
                    text.WriteLine("Bcc: {0}", string.Join("; ", _mimeMessage.Bcc.Mailboxes));
                text.WriteLine("Subject: {0}", _mimeMessage.Subject);
                var attachmentCount = _mimeMessage.Attachments.Count();
                if (attachmentCount > 0)
                    text.WriteLine("{0} Attachment{1}: '{2}'",
                        attachmentCount, attachmentCount == 1 ? "" : "s",
                        string.Join("', '", _mimeMessage.Attachments.GetAttachmentNames()));
                envelope = text.ToString();
            }
            return envelope;
        }
    }
}
