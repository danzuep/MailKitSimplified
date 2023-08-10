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
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MailKitSimplified.Sender.Abstractions;
using MailKitSimplified.Sender.Helpers;
using MailKitSimplified.Sender.Extensions;
using MailKitSimplified.Sender.Models;

namespace MailKitSimplified.Sender.Services
{
    /// <inheritdoc cref="IEmailWriter" />
    public class EmailWriter : IEmailWriter
    {
        internal const string TemplateName = "template.eml";
        public MimeMessage MimeMessage { get; private set; } = new MimeMessage();
        public MimeMessage Template { get; private set; } = null;
        private Func<IEmailWriter, Exception, Task<IEmailWriter>> _customExceptionMethod;
        private EmailWriterOptions _options;

        private readonly ILogger _logger;
        private readonly ISmtpSender _emailClient;
        private readonly IFileSystem _fileSystem;

        /// <inheritdoc cref="IEmailWriter" />
        public EmailWriter(ISmtpSender emailClient, ILogger<EmailWriter> logger = null, IOptions<EmailWriterOptions> options = null, IFileSystem fileSystem = null)
        {
            _logger = logger ?? NullLogger<EmailWriter>.Instance;
            _emailClient = emailClient ?? throw new ArgumentNullException(nameof(emailClient));
            _fileSystem = fileSystem ?? new FileSystem();
            _options = options?.Value ?? new EmailWriterOptions();
            _customExceptionMethod = (email, ex) =>
            {
                _logger.LogTrace(ex, $"Exception message ready to be sent by {_emailClient}.");
                email.Subject(ex.Message);
                return Task.FromResult(email);
            };
        }

        public IEmailWriter SetOptions(EmailWriterOptions options)
        {
            _options = options ?? new EmailWriterOptions();
            return this;
        }

        public IEmailWriter SetTemplate(MimeMessage mimeMessage)
        {
            Template = mimeMessage;
            MimeMessage = mimeMessage;
            return this;
        }

        public IEmailWriter From(string name, string address)
        {
            var fromMailboxAddress = new MailboxAddress(name, address);
            MimeMessage.From.Add(fromMailboxAddress);
            return this;
        }

        public IEmailWriter From(string addresses)
        {
            var mailboxAddresses = MailboxAddressHelper.ParseEmailContacts(addresses);
            MimeMessage.From.AddRange(mailboxAddresses);
            return this;
        }

        public IEmailWriter ReplyTo(string name, string address)
        {
            MimeMessage.ReplyTo.Add(new MailboxAddress(name, address));
            return this;
        }

        public IEmailWriter ReplyTo(string addresses)
        {
            var mailboxAddresses = MailboxAddressHelper.ParseEmailContacts(addresses);
            MimeMessage.ReplyTo.AddRange(mailboxAddresses);
            return this;
        }

        public IEmailWriter To(string name, string address)
        {
            MimeMessage.To.Add(new MailboxAddress(name, address));
            return this;
        }

        public IEmailWriter To(string addresses)
        {
            var mailboxAddresses = MailboxAddressHelper.ParseEmailContacts(addresses);
            MimeMessage.To.AddRange(mailboxAddresses);
            return this;
        }

        public IEmailWriter Cc(string name, string address)
        {
            MimeMessage.Cc.Add(new MailboxAddress(name, address));
            return this;
        }

        public IEmailWriter Cc(string addresses)
        {
            var mailboxAddresses = MailboxAddressHelper.ParseEmailContacts(addresses);
            MimeMessage.Cc.AddRange(mailboxAddresses);
            return this;
        }

        public IEmailWriter Bcc(string name, string address)
        {
            MimeMessage.Bcc.Add(new MailboxAddress(name, address));
            return this;
        }

        public IEmailWriter Bcc(string addresses)
        {
            var mailboxAddresses = MailboxAddressHelper.ParseEmailContacts(addresses);
            MimeMessage.Bcc.AddRange(mailboxAddresses);
            return this;
        }

        public IEmailWriter Subject(string subject)
        {
            MimeMessage.Subject = subject ?? string.Empty;
            return this;
        }

        public IEmailWriter Subject(string prefix, string suffix)
        {
            var original = MimeMessage.Subject ?? string.Empty;
            MimeMessage.Subject = $"{prefix}{original}{suffix}";
            return this;
        }

        public IEmailWriter BodyText(string textPlain) => Body(textPlain, false);

        public IEmailWriter BodyHtml(string textHtml) => Body(textHtml, true);

        private IEmailWriter Body(string bodyText, bool isHtml)
        {
            if (MimeMessage.Body == null)
            {
                var format = isHtml ? TextFormat.Html : TextFormat.Plain;
                MimeMessage.Body = new TextPart(format) { Text = bodyText ?? "" };
            }
            else
            {
                var builder = BuildMessage(MimeMessage);
                if (isHtml)
                    builder.HtmlBody = bodyText;
                else
                    builder.TextBody = bodyText;
                MimeMessage.Body = builder.ToMessageBody();
            }
            return this;
        }

        public IEmailWriter Configure(Func<IEmailWriter, Exception, Task<IEmailWriter>> customExceptionMethod)
        {
            _customExceptionMethod = customExceptionMethod;
            return this;
        }

        public IEmailWriter Exception(Exception exception)
        {
            if (_customExceptionMethod != null)
                _customExceptionMethod(this, exception);
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
            if (MimeMessage.Body == null)
            {
                MimeMessage.Body = mimeEntity;
            }
            else if (mimeEntity != null)
            {
                var builder = BuildMessage(MimeMessage);
                if (!linkedResource)
                    builder.Attachments.Add(mimeEntity);
                else
                    builder.LinkedResources.Add(mimeEntity);
                MimeMessage.Body = builder.ToMessageBody();
            }
            return this;
        }

        public IEmailWriter Attach(IEnumerable<MimeEntity> mimeEntities, bool linkedResource = false)
        {
            if (mimeEntities != null && mimeEntities.Any())
            {
                var builder = BuildMessage(MimeMessage);
                foreach (var mimePart in mimeEntities)
                    if (!linkedResource)
                        builder.Attachments.Add(mimePart);
                    else
                        builder.LinkedResources.Add(mimePart);
                MimeMessage.Body = builder.ToMessageBody();
            }
            return this;
        }

        public IEmailWriter TryAttach(params string[] filePaths)
        {
            if (filePaths != null)
            {
                var builder = BuildMessage(MimeMessage);
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
                MimeMessage.Body = builder.ToMessageBody();
            }
            return this;
        }

        public IEmailWriter Header(string field, string value)
        {
            MimeMessage.Headers.Add(field, value);
            return this;
        }

        public IEmailWriter Priority(MessagePriority priority)
        {
            MimeMessage.Priority = priority;
            return this;
        }

        public IEmailWriter SaveTemplate(CancellationToken cancellationToken = default)
        {
            Template = MimeMessage.Copy(cancellationToken);
            return this;
        }

        public async Task<IEmailWriter> SaveTemplateAsync(string fileName = TemplateName, CancellationToken cancellationToken = default)
        {
            Template = await MimeMessage.CopyAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(fileName))
                fileName = $"{MimeMessage.MessageId}.eml";
            await Template.WriteToAsync(fileName, cancellationToken).ConfigureAwait(false);
            return this;
        }

        public void Send(CancellationToken cancellationToken = default) =>
            SendAsync(cancellationToken).ConfigureAwait(false).GetAwaiter().GetResult();

        public async Task SendAsync(CancellationToken cancellationToken = default, ITransferProgress transferProgress = null)
        {
            await _emailClient.SendAsync(MimeMessage, cancellationToken, transferProgress).ConfigureAwait(false);
            if (Template == null && !string.IsNullOrEmpty(_options.TemplateFilePath) && File.Exists(_options.TemplateFilePath))
            {
                Template = await MimeMessage.LoadAsync(_options.TemplateFilePath, cancellationToken).ConfigureAwait(false);
                _logger.LogDebug($"Saved email {_options.TemplateFilePath} loaded as a template by {_emailClient}.");
            }
            MimeMessage = Template != null ? await Template.CopyAsync(cancellationToken) : new MimeMessage();
        }

        public bool TrySend(CancellationToken cancellationToken = default) =>
            TrySendAsync(cancellationToken).ConfigureAwait(false).GetAwaiter().GetResult();

        public async Task<bool> TrySendAsync(CancellationToken cancellationToken = default, ITransferProgress transferProgress = null)
        {
            bool isSent = await _emailClient.TrySendAsync(MimeMessage, cancellationToken, transferProgress).ConfigureAwait(false);
            MimeMessage = Template != null ? await Template.CopyAsync(cancellationToken) : new MimeMessage();
            return isSent;
        }

        public IEmailWriter Copy() => MemberwiseClone() as IEmailWriter;

        public override string ToString()
        {
            string envelope = string.Empty;
            using (var text = new StringWriter())
            {
                text.WriteLine("Message-Id: <{0}>", MimeMessage.MessageId);
                text.WriteLine("Date: {0}", MimeMessage.Date);
                if (MimeMessage.From.Count > 0)
                    text.WriteLine("From: {0}", string.Join("; ", MimeMessage.From.Mailboxes));
                if (MimeMessage.To.Count > 0)
                    text.WriteLine("To: {0}", string.Join("; ", MimeMessage.To.Mailboxes));
                if (MimeMessage.Cc.Count > 0)
                    text.WriteLine("Cc: {0}", string.Join("; ", MimeMessage.Cc.Mailboxes));
                if (MimeMessage.Bcc.Count > 0)
                    text.WriteLine("Bcc: {0}", string.Join("; ", MimeMessage.Bcc.Mailboxes));
                text.WriteLine("Subject: {0}", MimeMessage.Subject);
                var attachmentCount = MimeMessage.Attachments.Count();
                if (attachmentCount > 0)
                    text.WriteLine("{0} Attachment{1}: '{2}'",
                        attachmentCount, attachmentCount == 1 ? "" : "s",
                        string.Join("', '", MimeMessage.Attachments.GetAttachmentNames()));
                envelope = text.ToString();
            }
            return envelope;
        }
    }
}
