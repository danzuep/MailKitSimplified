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
using System.Diagnostics.CodeAnalysis;
using MailKitSimplified.Receiver.Extensions;

namespace MailKitSimplified.Receiver.Services
{
    [ExcludeFromCodeCoverage]
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
                throw new NullReferenceException("Mail folder property not available.");
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
            var sender = _mimeMessage.Sender ?? From.FirstOrDefault();
            var name = sender != null ? !string.IsNullOrEmpty(sender.Name) ?
                sender.Name : sender.Address : "someone";
            return $"On {Sent:f}, {name} wrote:";
        }

        public void Save(string name = "message.eml", bool useDosFormat = false)
        {
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

        private async Task<MimeMessage> CloneStreamReferences(bool persistent, CancellationToken cancellationToken = default)
        {
            using (var memory = new MemoryBlockStream())
            {
                _mimeMessage.WriteTo(memory);
                memory.Position = 0;
                var result = await MimeMessage.LoadAsync(memory, persistent, cancellationToken).ConfigureAwait(false);
                return result;
            }
        }

        public async Task<MimeMessage> Copy(CancellationToken cancellationToken = default) =>
            await CloneStreamReferences(true, cancellationToken).ConfigureAwait(false);

        public async Task<MimeMessage> Clone(CancellationToken ct = default) =>
            await CloneStreamReferences(false, ct).ConfigureAwait(false);

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
