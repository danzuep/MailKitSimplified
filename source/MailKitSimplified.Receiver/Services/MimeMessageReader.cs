﻿using MimeKit;
using MailKit;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using MailKitSimplified.Receiver.Models;
using MailKitSimplified.Receiver.Extensions;
using MimeKit.Text;
using MailKitSimplified.Receiver.Abstractions;

namespace MailKitSimplified.Receiver.Services
{
    public class MimeMessageReader : IDisposable
    {
        private Lazy<MimeMessage> _mimeMessage = new Lazy<MimeMessage>(() => new MimeMessage());
        protected MimeMessage MimeMessage { get => _mimeMessage.Value; }
        public string FolderName { get; private set; }
        public uint FolderIndex { get; private set; }

        public string MessageId { get => MimeMessage.MessageId ?? string.Empty; }
        public DateTimeOffset Date { get => MimeMessage.Date; }
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

        private IMailFolderReader _mailFolderReader;

        private MimeMessageReader() { }

        public MimeMessageReader(IMailFolderReader mailFolderReader)
        {
            _mailFolderReader = mailFolderReader ?? throw new NullReferenceException(nameof(mailFolderReader));
        }

        public static MimeMessageReader Create(MimeMessage mimeMessage, string mailFolderName = "", uint folderIndex = 0)
        {
            var mimeMessageReader = new MimeMessageReader
            {
                _mimeMessage = new Lazy<MimeMessage>(() => mimeMessage),
                FolderName = mailFolderName ?? string.Empty,
                FolderIndex = folderIndex
            };
            return mimeMessageReader;
        }

        public async Task GetMimeMessageAsync(IMessageSummary messageSummary, CancellationToken ct)
        {
            if (_mailFolderReader == null)
                throw new NullReferenceException(nameof(_mailFolderReader));
            var mailFolder = messageSummary.Folder;
            FolderName = mailFolder?.FullName ?? string.Empty;
            FolderIndex = messageSummary?.UniqueId.Id ?? 0;
            var mimeMessage = await _mailFolderReader.GetMimeMessageAsync(messageSummary.UniqueId, ct).ConfigureAwait(false);
            _mimeMessage = new Lazy<MimeMessage>(() => mimeMessage);
        }

        public static string DecodeHtmlBody(string html)
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

        internal string GetOnDateSenderWrote()
        {
            var sender = _mimeMessage.Value.Sender ?? From.FirstOrDefault();
            var name = sender != null ? !string.IsNullOrEmpty(sender.Name) ?
                sender.Name : sender.Address : "someone";

            return string.Format("On {0}, {1} wrote:", Date.ToString("f"), name);
        }

        internal static string QuoteText(string text, string prefix = "")
        {
            using (var quoted = new StringWriter())
            {
                if (!string.IsNullOrEmpty(prefix))
                {
                    quoted.WriteLine(prefix);
                }

                if (!string.IsNullOrEmpty(text))
                {
                    using (var reader = new StringReader(text))
                    {
                        string line;

                        while ((line = reader.ReadLine()) != null)
                        {
                            quoted.Write("> ");
                            quoted.WriteLine(line);
                        }
                    }
                }

                return quoted.ToString();
            }
        }

        public static async Task<Stream> WriteToStreamAsync(MimeEntity entity, Stream stream, CancellationToken ct = default)
        {
            if (entity is MessagePart messagePart)
            {
                await messagePart.Message.WriteToAsync(stream, ct);
            }
            else if (entity is MimePart mimePart && mimePart.Content != null)
            {
                await mimePart.Content.DecodeToAsync(stream, ct);
            }
            // rewind the stream so the next process can read it from the beginning
            stream.Position = 0;
            return stream;
        }

        public override string ToString()
        {
            string output = string.Empty;
            using (var text = new StringWriter())
            {
                text.Write("'{0}' {1}. ", FolderName, FolderIndex);
                //text.Write("{0} Attachment(s){1}. ", AttachmentCount, AttachmentNamesEnumerated);
                text.Write("Sent: {0}. Received: {1}. Subject: '{2}'.", Date, DateTime.Now, Subject);
                output = text.ToString();
            }
            return output;
        }

        public void Dispose()
        {
            if (_mimeMessage?.IsValueCreated ?? false)
                _mimeMessage = null;
        }
    }
}
