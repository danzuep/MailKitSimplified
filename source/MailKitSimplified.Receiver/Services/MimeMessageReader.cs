using MimeKit;
using MimeKit.IO;
using MimeKit.Text;
using MailKit;
using MailKit.Net.Imap;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MailKitSimplified.Receiver.Extensions;

namespace MailKitSimplified.Receiver.Services
{
    public class MimeMessageReader
    {
        private MimeMessage _mimeMessage = new MimeMessage();
        protected MimeMessage MimeMessage { get => _mimeMessage; }
        public string FolderName { get; private set; }
        public uint FolderIndex { get; private set; }

        public string MessageId { get => MimeMessage.MessageId ?? string.Empty; }
        public DateTimeOffset Sent { get => MimeMessage.Date; }
        public IEnumerable<MailboxAddress> From { get => MimeMessage.From.Mailboxes; }
        public IEnumerable<MailboxAddress> To { get => MimeMessage.To.Mailboxes; }
        public IEnumerable<MailboxAddress> Cc { get => MimeMessage.Cc.Mailboxes; }
        public IEnumerable<MailboxAddress> Bcc { get => MimeMessage.Bcc.Mailboxes; }
        public IEnumerable<MailboxAddress> ResentFrom { get => MimeMessage.ResentFrom.Mailboxes; }
        public IEnumerable<MimeEntity> Attachments { get => MimeMessage.Attachments; }
        public IEnumerable<string> AttachmentNames { get => Attachments.GetAttachmentNames(); }
        public int AttachmentCount { get => Attachments.Count(); }
        public string AttachmentNamesEnumerated { get => AttachmentNames.ToEnumeratedString(); }
        public string Subject { get => MimeMessage.Subject ?? string.Empty; }
        public bool IsHtml { get => MimeMessage.HtmlBody != null; }
        public string Body { get => MimeMessage.HtmlBody ?? MimeMessage.TextBody ?? string.Empty; }
        public string BodyText { get => IsHtml ? DecodeHtmlBody(Body) : MimeMessage.TextBody ?? string.Empty; }
        private static ILogger _logger = NullLogger<MimeMessageReader>.Instance;

        private MimeMessageReader() { }

        public static MimeMessageReader Create(MimeMessage mimeMessage, string mailFolderName = null, uint folderIndex = 0)
        {
            var mimeMessageReader = new MimeMessageReader
            {
                _mimeMessage = mimeMessage ?? throw new ArgumentNullException(nameof(mimeMessage)),
                FolderName = mailFolderName ?? string.Empty,
                FolderIndex = folderIndex
            };
            return mimeMessageReader;
        }

        public MimeMessageReader SetLogger(ILogger logger)
        {
            if (logger != null)
                _logger = logger;
            return this;
        }

        public MimeMessageReader SetLogger(ILoggerFactory loggerFactory)
        {
            if (loggerFactory != null)
                _logger = loggerFactory.CreateLogger<MimeMessageReader>();
            return this;
        }

        public MimeMessageReader SetLogger(Action<ILoggingBuilder> configure = null)
        {
            var loggerFactory = configure != null ? LoggerFactory.Create(configure) :
                LoggerFactory.Create(_ => _.SetMinimumLevel(LogLevel.Debug).AddDebug().AddConsole());
            _logger = loggerFactory.CreateLogger<MimeMessageReader>();
            return this;
        }

        /// <exception cref="MessageNotFoundException">Message was moved before it could be downloaded</exception>
        /// <exception cref="ImapCommandException">Message was moved before it could be downloaded</exception>
        /// <exception cref="FolderNotOpenException">Mail folder was closed</exception>
        /// <exception cref="IOException">Message not downloaded</exception>
        /// <exception cref="ImapProtocolException">Message not downloaded</exception>
        /// <exception cref="InvalidOperationException">Message not downloaded</exception>
        /// <exception cref="OperationCanceledException">Message download task was cancelled.</exception>
        public static async Task<MimeMessageReader> CreateAsync(IMessageSummary messageSummary, CancellationToken cancellationToken = default)
        {
            if (messageSummary == null)
                throw new ArgumentNullException(nameof(messageSummary));
            var mailFolder = messageSummary.Folder;
            if (mailFolder == null)
                throw new ArgumentException("Mail folder property not available.");
            bool closeWhenFinished = !mailFolder.IsOpen;
            if (closeWhenFinished)
                _ = await mailFolder.OpenAsync(FolderAccess.ReadOnly, cancellationToken).ConfigureAwait(false);
            var uniqueId = messageSummary.UniqueId;
            var mimeMessage = await mailFolder.GetMessageAsync(uniqueId, cancellationToken).ConfigureAwait(false);
            var mimeMessageReader = Create(mimeMessage, mailFolder.FullName, uniqueId.Id);
            if (mailFolder.Access == FolderAccess.ReadWrite)
                await mailFolder.AddFlagsAsync(uniqueId, MessageFlags.Seen, true, cancellationToken);
            if (closeWhenFinished)
                await mailFolder.CloseAsync(false, CancellationToken.None).ConfigureAwait(false);
            return mimeMessageReader;
        }

        public static string DecodeHtmlBody(string html, CancellationToken cancellationToken = default)
        {
            if (html == null)
                return string.Empty;

            bool previousWasNewLine = false;
            using (var writer = new StringWriter())
            {
                using (var reader = new StringReader(html))
                {
                    var tokenizer = new HtmlTokenizer(reader)
                    {
                        DecodeCharacterReferences = true
                    };

                    while (tokenizer.ReadNextToken(out HtmlToken token))
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        switch (token.Kind)
                        {
                            case HtmlTokenKind.Data:
                                var data = token as HtmlDataToken;
                                if (!string.IsNullOrWhiteSpace(data?.Data) &&
                                    !data.Data.StartsWith("<!--"))
                                {
                                    writer.Write(data.Data);
                                    if (!data.Data.EndsWith(Environment.NewLine))
                                        previousWasNewLine = false;
                                }
                                break;
                            case HtmlTokenKind.Tag:
                                var tag = (HtmlTagToken)token;
                                switch (tag.Id)
                                {
                                    case HtmlTagId.BlockQuote:
                                    case HtmlTagId.Br:
                                        if (!previousWasNewLine)
                                        {
                                            writer.Write(Environment.NewLine);
                                            previousWasNewLine = true;
                                        }
                                        break;
                                    case HtmlTagId.P:
                                        if (!previousWasNewLine &&
                                            tag.IsEndTag || tag.IsEmptyElement)
                                        {
                                            writer.Write(Environment.NewLine);
                                            previousWasNewLine = true;
                                        }
                                        break;
                                }
                                break;
                        }
                    }
                }
                return writer.ToString();
            }
        }

        public static IList<string> DecodeHtmlHrefs(string html, CancellationToken cancellationToken = default)
        {
            if (html == null)
                return null;

            var hrefs = new List<string>();
            using (var reader = new StringReader(html))
            {
                var tokenizer = new HtmlTokenizer(reader)
                {
                    DecodeCharacterReferences = true
                };

                while (tokenizer.ReadNextToken(out HtmlToken token))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (token.Kind == HtmlTokenKind.Tag &&
                        token is HtmlTagToken tag &&
                        tag.Id == HtmlTagId.A &&
                        tag.Attributes != null &&
                        tag.Attributes.Any())
                    {
                        hrefs.AddRange(tag.Attributes.Select(a => a.Value));
                    }
                }
            }

            return hrefs;
        }

        internal string GetOnDateSenderWrote()
        {
            var sender = _mimeMessage.Sender ?? From.FirstOrDefault();
            var name = sender != null ? !string.IsNullOrEmpty(sender.Name) ?
                sender.Name : sender.Address : "someone";
            return $"On {Sent:f}, {name} wrote:";
        }

        public void Save(string name = null, bool useDosFormat = false)
        {
            if (string.IsNullOrWhiteSpace(name))
                name = $"{_mimeMessage.MessageId}.eml";
            if (useDosFormat)
            {
                var format = FormatOptions.Default.Clone();
                format.NewLineFormat = NewLineFormat.Dos;
                _mimeMessage.WriteTo(format, name);
            }
            else
            {
                _mimeMessage.WriteTo(name);
            }
        }

        public async Task SaveAsync(string fileName = null, bool useUnixNewLine = false, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                fileName = $"{_mimeMessage.MessageId}.eml";
            var format = FormatOptions.Default.Clone();
            format.NewLineFormat = useUnixNewLine ? NewLineFormat.Unix : NewLineFormat.Dos;
            await _mimeMessage.WriteToAsync(format, fileName, cancellationToken).ConfigureAwait(false);
        }

        public async Task<IList<string>> DownloadAllAttachmentsAsync(string downloadFolderPath, bool createDirectory = false, CancellationToken cancellationToken = default)
        {
            if (createDirectory)
                Directory.CreateDirectory(downloadFolderPath);
            else if (!Directory.Exists(downloadFolderPath))
                throw new DirectoryNotFoundException($"Directory not found: {downloadFolderPath}.");
            IList<string> downloads = new List<string>();
            if (_mimeMessage.Attachments != null && !string.IsNullOrEmpty(downloadFolderPath))
            {
                _logger.LogDebug("Downloading attachments to '{FilePath}'.", downloadFolderPath);
                foreach (MimePart attachment in _mimeMessage.Attachments)
                {
                    string filePath = Path.Combine(downloadFolderPath, attachment.FileName);
                    using (FileStream stream = File.OpenWrite(filePath))
                    {
                        await attachment.WriteToAsync(stream, cancellationToken);
                        _logger.LogDebug("{FileName} downloaded.", attachment.FileName);
                    }
                    downloads.Add(filePath);
                }
            }
            return downloads;
        }

        public override string ToString()
        {
            string envelope = string.Empty;
            using (var text = new StringWriter())
            {
                text.WriteLine("'{0}' {1}", FolderName, FolderIndex);
                text.WriteLine("Message-Id: <{0}>", MessageId);
                text.WriteLine("Sent: {0}", Sent);
                text.WriteLine("Received: {0}", DateTime.Now);
                if (MimeMessage.From.Count > 0)
                    text.WriteLine("From: {0}", string.Join("; ", From));
                if (MimeMessage.To.Count > 0)
                    text.WriteLine("To: {0}", string.Join("; ", To));
                if (MimeMessage.Cc.Count > 0)
                    text.WriteLine("Cc: {0}", string.Join("; ", Cc));
                if (MimeMessage.Bcc.Count > 0)
                    text.WriteLine("Bcc: {0}", string.Join("; ", Bcc));
                text.WriteLine("Subject: {0}", Subject);
                if (AttachmentCount > 0)
                    text.WriteLine("{0} Attachment{1}: '{2}'",
                        AttachmentCount, AttachmentCount == 1 ? "" : "s",
                        string.Join("', '", AttachmentNames));
                envelope = text.ToString();
            }
            return envelope;
        }
    }
}
