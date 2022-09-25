using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics;
using MimeKit;
using MimeKit.Text;
using MimeKit.Utils;
using MailKitSimplified.Sender.Abstractions;

namespace MailKitSimplified.Sender
{
    public class MimeEmail : IFluentEmail
    {
        private MimeMessage _mimeMessage = new MimeMessage();
        private IList<string> _attachmentFilePaths = new List<string>();

        private readonly IMimeEmailSender _emailClient;

        public MimeEmail(IMimeEmailSender emailClient)
        {
            _emailClient = emailClient ?? throw new ArgumentNullException(nameof(emailClient));
        }

        public IFluentEmail From(string address, string name = "")
        {
            _mimeMessage.From.Add(new MailboxAddress(name, address));
            return this;
        }

        public IFluentEmail To(string address, string name = "")
        {
            _mimeMessage.To.Add(new MailboxAddress(name, address));
            return this;
        }

        public IFluentEmail Subject(string subject)
        {
            _mimeMessage.Subject = subject ?? string.Empty;
            return this;
        }

        public IFluentEmail Body(string bodyText, bool isHtml = true)
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

        public IFluentEmail Attach(params string[] filePaths)
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

        public bool CheckFileExists(string filePath)
        {
            bool isExisting = false;

            var directory = Path.GetDirectoryName(filePath);

            if (!Directory.Exists(directory))
                Trace.TraceWarning($"Folder not found: '{directory}'");
            else if (!File.Exists(filePath))
                Trace.TraceWarning($"File not found: '{filePath}'");
            else
                isExisting = true;

            return isExisting;
        }

        public async Task<MemoryStream> GetMemoryStreamAsync(string filePath, CancellationToken cancellationToken = default)
        {
            const int BufferSize = 8192;
            var outputStream = new MemoryStream();
            if (CheckFileExists(filePath))
            {
                using (var source = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, useAsync: true))
                {
                    await source.CopyToAsync(outputStream, BufferSize, cancellationToken);
                };
                outputStream.Position = 0;
                Trace.TraceInformation($"OK '{filePath}'");
            }
            return outputStream;
        }

        public MimePart GetMimePart(Stream stream, string fileName, string contentType = System.Net.Mime.MediaTypeNames.Application.Octet)
        {
            MimePart attachment = null;
            if (stream?.Length > 0)
            {
                attachment = new MimePart(contentType)
                {
                    FileName = fileName ?? "",
                    Content = new MimeContent(stream),
                    ContentTransferEncoding = ContentEncoding.Base64,
                    ContentDisposition = new ContentDisposition(
                        ContentDisposition.Attachment),
                    ContentId = MimeUtils.GenerateMessageId()
                };
            }
            return attachment;
        }

        public async Task<IEnumerable<MimePart>> LoadAttachmentsAsync(IEnumerable<string> filePaths, CancellationToken cancellationToken = default)
        {
            IList<MimePart> attachments = new List<MimePart>();
            if (filePaths != null)
            {
                foreach (var filePath in filePaths)
                {
                    var stream = await GetMemoryStreamAsync(filePath, cancellationToken);
                    var fileName = Path.GetFileName(filePath ?? "");
                    var mimePart = GetMimePart(stream, fileName);
                    if (mimePart != null)
                        attachments.Add(mimePart);
                }
            }
            return attachments;
        }

        private async Task AddAttachments(CancellationToken cancellationToken = default)
        {
            var attachments = await LoadAttachmentsAsync(_attachmentFilePaths, cancellationToken);
            if (attachments.Any())
            {
                var multipart = new Multipart();
                if (_mimeMessage.Body != null)
                    multipart.Add(_mimeMessage.Body);
                foreach (var attachment in attachments)
                    multipart.Add(attachment);
                _mimeMessage.Body = multipart;
            }
        }

        public async Task SendAsync(CancellationToken cancellationToken = default)
        {
            await AddAttachments(cancellationToken);
            await _emailClient.SendAsync(_mimeMessage, cancellationToken);
        }
    }
}
