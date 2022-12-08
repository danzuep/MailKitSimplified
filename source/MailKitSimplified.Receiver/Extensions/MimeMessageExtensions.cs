using MimeKit;
using MimeKit.Text;
using MailKit;
using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace MailKitSimplified.Receiver.Extensions
{
    [ExcludeFromCodeCoverage]
    public static class MimeMessageExtensions
    {
        public static readonly string RE = "RE: ";
        public static readonly string FW = "FW: ";

        public static MimeMessage GetReplyMessage(this MimeMessage original, string message, string from, string replyTo = null) =>
            original.GetReplyMessage(message, ParseMailboxAddress(from), replyTo == null ? null : ParseMailboxAddress(replyTo));

        public static MimeMessage GetReplyMessage(this MimeMessage original, string message, IEnumerable<MailboxAddress> from, IEnumerable<MailboxAddress> replyTo = null)
        {
            var mimeMessage = original.GetReplyMessage(message, addRecipients: true);
            mimeMessage.From.AddRange(from);
            if (replyTo != null)
                mimeMessage.ReplyTo.AddRange(replyTo);
            return mimeMessage;
        }

        public static MimeMessage GetForwardMessage(this MimeMessage original, string message, string from, string to, string replyTo = null) =>
            original.GetForwardMessage(message, ParseMailboxAddress(from), ParseMailboxAddress(to), replyTo == null ? null : ParseMailboxAddress(replyTo));

        public static MimeMessage GetForwardMessage(this MimeMessage original, string message, IEnumerable<MailboxAddress> from, IEnumerable<MailboxAddress> to, IEnumerable<MailboxAddress> replyTo = null)
        {
            var mimeMessage = original.GetForwardMessage(message);
            mimeMessage.From.AddRange(from);
            mimeMessage.To.AddRange(to);
            if (replyTo != null)
                mimeMessage.ReplyTo.AddRange(replyTo);
            return mimeMessage;
        }

        public static MimeMessage GetForwardMessage(this MimeMessage original, string message) =>
            original.GetMimeMessageResponse(RE, message, includeAttachments: true);

        public static MimeMessage GetReplyMessage(this MimeMessage original, string message, bool addRecipients = false)
        {
            var mimeMessage = original.GetMimeMessageResponse(RE, message, includeAttachments: false);
            if (addRecipients)
                mimeMessage.To.AddRange(original.BuildReplyAddresses(replyToAll: false));
            return mimeMessage;
        }

        internal static MimeMessage GetMimeMessageResponse(this MimeMessage original, string subjectPrefix = "", string bodyPrefix = "", bool includeAttachments = true, bool includeEmbedded = true)
        {
            if (original == null)
                throw new ArgumentNullException(nameof(original));

            var mimeMessage = new MimeMessage();

            // Set the subject with prefix check
            mimeMessage.Subject = GetPrefixedSubject(original.Subject, subjectPrefix);

            // Construct the In-Reply-To and References headers
            mimeMessage.AddMessageIdReferences(original);

            // Quote the original message text with optional linked resources and attachments
            mimeMessage.Body = original.BuildMessageBody(bodyPrefix, includeAttachments, includeEmbedded);

            return mimeMessage;
        }

        public static IEnumerable<MailboxAddress> ParseMailboxAddress(string value)
        {
            char[] separator = new char[] { ';', ',', ' ', '|' };
            return string.IsNullOrEmpty(value) ? Array.Empty<MailboxAddress>() :
                value.Split(separator, StringSplitOptions.RemoveEmptyEntries)
                    .Select(f => new MailboxAddress(string.Empty, f));
        }

        private static string GetPrefixedSubject(string originalSubject, string prefix = "")
        {
            string subject = originalSubject ?? string.Empty;
            if (!string.IsNullOrEmpty(prefix) && !subject.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                subject = $"{prefix}{originalSubject}";
            return subject;
        }

        private static InternetAddressList BuildReplyAddresses(this MimeMessage original, bool replyToAll = false)
        {
            if (original == null)
                throw new ArgumentNullException(nameof(original));

            var to = new InternetAddressList();

            if (original.ResentFrom.Count > 0)
                to.AddRange(original.ResentFrom);
            else if (original.ReplyTo.Count > 0)
                to.AddRange(original.ReplyTo);
            else if (original.From.Count > 0)
                to.AddRange(original.From);
            else if (original.Sender != null)
                to.Add(original.Sender);

            if (replyToAll && original.ResentFrom.Count == 0)
                to.AddRange(original.GetRecipients(onlyUnique: true));

            return to;
        }

        internal static bool Contains(this IEnumerable<MailboxAddress> mailboxA, IEnumerable<MailboxAddress> mailboxB) =>
            mailboxA.Any(a => !mailboxB.Any(b => a.Address.Equals(b.Address, StringComparison.OrdinalIgnoreCase)));

        internal static IEnumerable<MailboxAddress> Excluding(this IEnumerable<MailboxAddress> mailboxA, IEnumerable<MailboxAddress> mailboxB) =>
            mailboxA.Where(a => !mailboxB.Any(b => a.Address.Equals(b.Address, StringComparison.OrdinalIgnoreCase)));

        internal static void AddMessageIdReferences(this MimeMessage mimeMessage, MimeMessage original)
        {
            if (!string.IsNullOrEmpty(original?.MessageId))
            {
                mimeMessage.InReplyTo = original.MessageId;
                foreach (var id in original.References)
                    mimeMessage.References.Add(id);
                mimeMessage.References.Add(original.MessageId);
            }
        }

        internal static BodyBuilder GetBuilder(this MimeMessage original, bool includeAttachments = true, bool includeEmbedded = true)
        {
            var builder = new BodyBuilder();
            if (includeEmbedded)
            {
                var linkedResources = original.BodyParts
                    .Where(part => !part.IsAttachment && part.ContentId != null &&
                        ((original.HtmlBody?.Contains(part.ContentId) ?? false) ||
                        (original.TextBody?.Contains(part.ContentId) ?? false)));
                foreach (var resource in linkedResources)
                    builder.LinkedResources.Add(resource);
            }
            if (includeAttachments)
            {
                foreach (var attachment in original.Attachments)
                    builder.Attachments.Add(attachment);
            }
            return builder;
        }

        internal static MimeEntity BuildMessageBody(this MimeMessage original, string prependText = "", bool includeAttachments = true, bool includeEmbedded = true, bool setHtml = true)
        {
            if (original == null)
                return new TextPart();
            MimeEntity mimeBody;
            bool isHtml = setHtml || original.HtmlBody != null;
            var replyText = original.QuoteForReply(prependText);
            if (includeEmbedded || includeAttachments)
            {
                var builder = original.GetBuilder(includeAttachments, includeEmbedded);
                if (isHtml)
                    builder.HtmlBody = replyText;
                else
                    builder.TextBody = replyText;
                mimeBody = builder.ToMessageBody();
            }
            else
            {
                var format = isHtml ? TextFormat.Html : TextFormat.Plain;
                mimeBody = new TextPart(format) { Text = replyText };
            }
            return mimeBody;
        }

        public static MimeEntity BuildMultipart(this MimeEntity mimeBody, params MimeEntity[] mimeEntities)
        {
            if (mimeEntities == null || mimeEntities.Length == 0)
                return mimeBody;

            var multipart = new Multipart();
            if (mimeBody != null)
                multipart.Add(mimeBody);
            foreach (var mimeEntity in mimeEntities)
                if (mimeEntity != null)
                    multipart.Add(mimeEntity);

            return multipart;
        }

        /// <summary>
        /// Quote the original message and add a new message above it.
        /// </summary>
        /// <param name="original"><see cref="MimeMessage"/> to quote.</param>
        /// <param name="message">New message to add above it.</param>
        /// <param name="htmlBorder">HTML border style.</param>
        /// <returns>Quoted message text.</returns>
        internal static string QuoteForReply(this MimeMessage original, string message = "", bool includeMessageId = false, CancellationToken cancellationToken = default)
        {
            if (original == null)
                throw new ArgumentNullException(nameof(original));
            cancellationToken.ThrowIfCancellationRequested();

            var stringBuilder = new StringBuilder();
            if (original.HtmlBody == null)
            {
                stringBuilder.AppendLine(message ?? string.Empty);
                stringBuilder.AppendLine();
                stringBuilder.AppendLine("---------- Original Message ----------");
                if (includeMessageId)
                    stringBuilder.AppendLine($"> Message-ID: <{original.MessageId}>");
                stringBuilder.AppendLine($"> Sent: {original.Date}");
                stringBuilder.AppendLine($"> From: {original.From}");
                if (original.ResentFrom.Count > 0)
                    stringBuilder.AppendLine($"> Resent From: {original.ResentFrom}");
                stringBuilder.AppendLine($"> To: {original.To}");
                if (original.Cc.Count > 0)
                    stringBuilder.AppendLine($"> Cc: {original.Cc}");
                stringBuilder.AppendLine($"> Subject: {original.Subject}");
                if (original.Attachments?.Any() ?? false)
                {
                    var attachmentCount = original.Attachments.Count();
                    var pluraliser = attachmentCount == 1 ? "" : "s";
                    var attachmentNames = original.Attachments.GetAttachmentNames().ToEnumeratedString("', '");
                    stringBuilder.AppendLine($"> {attachmentCount} Attachment{pluraliser}: '{attachmentNames}'");
                }
                stringBuilder.AppendLine("> ");
                if (!string.IsNullOrEmpty(original.TextBody))
                {
                    string nextLine;
                    using (var reader = new StringReader(original.TextBody))
                    {
                        while ((nextLine = reader.ReadLine()) != null)
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            stringBuilder.Append("> ");
                            stringBuilder.AppendLine(nextLine);
                        }
                    }
                    stringBuilder.AppendLine();
                }
            }
            else
            {
                string GetHtml(InternetAddressList contacts) => contacts.Mailboxes.Select(a => $"\"{a.Name}\" &lt;{a.Address}&gt;").ToEnumeratedString("; ");
                stringBuilder.AppendLine("<div>");
                stringBuilder.AppendLine(message ?? string.Empty);
                stringBuilder.AppendLine("</div><br /><blockquote><hr /><div>");
                if (includeMessageId)
                    stringBuilder.AppendLine($"<b>Message-ID:</b> &lt;{original.MessageId}&gt;<br />");
                stringBuilder.AppendLine($"<b>Sent:</b> {original.Date}<br />");
                stringBuilder.AppendLine($"<b>From:</b> {GetHtml(original.From)}<br />");
                if (original.ResentFrom.Count > 0)
                    stringBuilder.AppendLine($"<b>Resent From:</b> {GetHtml(original.ResentFrom)}<br />");
                stringBuilder.AppendLine($"<b>To:</b> {GetHtml(original.To)}<br />");
                if (original.Cc.Count > 0)
                    stringBuilder.AppendLine($"<b>Cc:</b> {GetHtml(original.Cc)}<br />");
                stringBuilder.AppendLine($"<b>Subject:</b> {original.Subject}<br />");
                if (original.Attachments?.Any() ?? false)
                {
                    var attachmentCount = original.Attachments.Count();
                    var pluraliser = attachmentCount == 1 ? "" : "s";
                    var attachmentNames = original.Attachments.GetAttachmentNames().ToEnumeratedString("', '");
                    stringBuilder.AppendLine($"<b>{attachmentCount} Attachment{pluraliser}:</b> '{attachmentNames}'<br />");
                }
                stringBuilder.AppendLine("</div><br />");
                stringBuilder.AppendLine(original.HtmlBody ?? string.Empty);
                stringBuilder.AppendLine("</blockquote>");
            }
            var result = stringBuilder.ToString();
            return result;
        }

        /// <summary>
        /// Downloads the text/html part of the body if it exists, otherwise downloads the text/plain part.
        /// </summary>
        /// <param name="messageSummary"><see cref="IMessageSummary"/> body to download.</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns><see cref="TextPart"/> of the message body <see cref="MimeEntity"/>.</returns>
        public static async Task<string> GetBodyTextAsync(this IMessageSummary messageSummary, CancellationToken cancellationToken = default)
        {
            MimeEntity textEntity = null;
            bool peekFolder = !messageSummary?.Folder?.IsOpen ?? false;
            if (peekFolder)
                await messageSummary.Folder.OpenAsync(FolderAccess.ReadOnly, cancellationToken);
            if (messageSummary?.HtmlBody != null)
                textEntity = await messageSummary.Folder.GetBodyPartAsync(messageSummary.UniqueId, messageSummary.HtmlBody, cancellationToken);
            else if (messageSummary?.TextBody != null)
                textEntity = await messageSummary.Folder.GetBodyPartAsync(messageSummary.UniqueId, messageSummary.TextBody, cancellationToken);
            if (peekFolder)
                await messageSummary.Folder.CloseAsync(false, cancellationToken);
            var textPart = textEntity as TextPart;
            return textPart?.Text ?? string.Empty;
        }

        /// <summary>
        /// Downloads detials for all attachments linked to this message.
        /// </summary>
        /// <param name="messageSummary"><see cref="IMessageSummary"/> attachments to download.</param>
        /// <param name="fileTypeSuffix">Optional attachment name file type suffix filter.</param>
        /// <param name="getAllNonText">Optional non-text media type filter.</param>
        /// <returns>Message body parts filtered down to just the attachments.</returns>
        public static IEnumerable<BodyPartBasic> GetMailAttachmentDetails(this IMessageSummary messageSummary, IList<string> fileTypeSuffix = null, bool getAllNonText = false)
        {
            IEnumerable<BodyPartBasic> attachments = Array.Empty<BodyPartBasic>();
            if (messageSummary?.Body is BodyPartMultipart multipart)
            {
                var attachmentParts = multipart.BodyParts.OfType<BodyPartBasic>().Where(p => p.IsAttachment || (getAllNonText && !p.ContentType.MediaType.ToLower().Contains("text")));
                attachments = fileTypeSuffix?.Count > 0 ? attachmentParts.Where(a => fileTypeSuffix.Any(s => a.FileName?.EndsWith(s, StringComparison.OrdinalIgnoreCase) ?? false)) : attachmentParts;
            }
            return attachments.ToList();
        }
    }
}
