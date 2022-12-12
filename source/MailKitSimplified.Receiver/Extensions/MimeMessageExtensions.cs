using MimeKit;
using MimeKit.Text;
using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Threading;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace MailKitSimplified.Receiver.Extensions
{
    [ExcludeFromCodeCoverage]
    public static class MimeMessageExtensions
    {
        private static readonly string RE = MessageSummaryExtensions.RE;
        private static readonly string FW = MessageSummaryExtensions.FW;

        /// <summary>
        /// Get a MimeMessage forward from a MimeMessage original.
        /// </summary>
        /// <param name="original">MimeMessage original to forward.</param>
        /// <param name="message">Forward message text/html body.</param>
        /// <returns>MimeMessage forward ready for From and To addresses.</returns>
        public static MimeMessage GetForwardMessage(this MimeMessage original, string message) =>
            original.GetMimeMessageResponse(FW, message, includeAttachments: true);

        /// <summary>
        /// Get a MimeMessage reply from a MimeMessage original.
        /// </summary>
        /// <param name="original">MimeMessage original to reply to.</param>
        /// <param name="message">Reply message text/html body.</param>
        /// <param name="addRecipients">Whether to reply to sender or not.</param>
        /// <param name="replyToAll">Whether to reply to all original recipients or not.</param>
        /// <returns>MimeMessage reply ready for From (and To) addresses.</returns>
        public static MimeMessage GetReplyMessage(this MimeMessage original, string message, bool addRecipients = true, bool replyToAll = false)
        {
            var mimeMessage = original.GetMimeMessageResponse(RE, message, includeAttachments: false);
            if (addRecipients)
                mimeMessage.To.AddRange(original.BuildReplyAddresses(replyToAll));
            return mimeMessage;
        }

        internal static MimeMessage GetMimeMessageResponse(this MimeMessage original, string subjectPrefix = "", string bodyPrefix = "", bool includeAttachments = true, bool includeEmbedded = true)
        {
            if (original == null)
                throw new ArgumentNullException(nameof(original));

            // Set the subject with prefix check
            var mimeMessage = new MimeMessage
            {
                Subject = MessageSummaryExtensions.GetPrefixedSubject(original.Subject, subjectPrefix)
            };

            // Construct the In-Reply-To and References headers
            mimeMessage.AddMessageIdReferences(original);

            // Quote the original message text with optional linked resources and attachments
            mimeMessage.Body = original.BuildMessageBody(bodyPrefix, includeAttachments, includeEmbedded);

            return mimeMessage;
        }

        internal static IEnumerable<MailboxAddress> ParseMailboxAddress(string value)
        {
            char[] separator = new char[] { ';', ',', ' ', '|' };
            return string.IsNullOrEmpty(value) ? Array.Empty<MailboxAddress>() :
                value.Split(separator, StringSplitOptions.RemoveEmptyEntries)
                    .Select(f => new MailboxAddress(string.Empty, f));
        }

        /// <summary>
        /// Adds the specified address(es) to the end of the address list.
        /// Multiple addresses can be separated with ';', ',', ' ', or '|'.
        /// </summary>
        /// <param name="addressList">Address list to add to.</param>
        /// <param name="emailAddress">Email address(es) to add.</param>
        /// <exception cref="ArgumentNullException">Email address is null.</exception>
        public static void Add(this InternetAddressList addressList, string emailAddress)
        {
            if (string.IsNullOrEmpty(emailAddress))
                throw new ArgumentNullException(nameof(emailAddress));
            var emailAddresses = ParseMailboxAddress(emailAddress);
            addressList.AddRange(emailAddresses);
        }

        internal static InternetAddressList BuildReplyAddresses(this MimeMessage original, bool replyToAll = false)
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

        /// <summary>
        /// Quote the original message and add a new message above it.
        /// var bodyText = await original.GetBodyTextAsync();
        /// </summary>
        /// <param name="original"><see cref="MimeMessage"/> to quote.</param>
        /// <param name="message">New message to add above it.</param>
        /// <param name="includeMessageId">Include Message-ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Quoted message text.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        internal static string QuoteForReply(this MimeMessage original, string message, bool includeMessageId = false, CancellationToken cancellationToken = default)
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
    }
}
