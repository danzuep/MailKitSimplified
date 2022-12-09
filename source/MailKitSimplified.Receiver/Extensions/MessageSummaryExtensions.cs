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
    public static class MessageSummaryExtensions
    {
        internal static readonly string RE = "RE: ";
        internal static readonly string FW = "FW: ";

        public static async Task<MimeMessage> GetForwardMessageAsync(this IMessageSummary original, string message) =>
            await original.GetMimeMessageResponseAsync(FW, message, includeAttachments: true);

        public static async Task<MimeMessage> GetReplyMessageAsync(this IMessageSummary original, string message, bool addRecipients = false)
        {
            var mimeMessage = await original.GetMimeMessageResponseAsync(RE, message, includeAttachments: false);
            if (addRecipients)
                mimeMessage.To.AddRange(original.BuildReplyAddresses(replyToAll: false));
            return mimeMessage;
        }

        internal static async Task<MimeMessage> GetMimeMessageResponseAsync(this IMessageSummary original, string subjectPrefix = "", string bodyPrefix = "", bool includeAttachments = true, bool includeEmbedded = true)
        {
            if (original == null)
                throw new ArgumentNullException(nameof(original));
            if (original.Envelope == null)
                throw new NullReferenceException("IMessageSummary.Envelope is null.");

            // Set the subject with prefix check
            var mimeMessage = new MimeMessage
            {
                Subject = GetPrefixedSubject(original.Envelope.Subject, subjectPrefix)
            };

            // Construct the In-Reply-To and References headers
            mimeMessage.AddMessageIdReferences(original);

            // Quote the original message text with optional linked resources and attachments
            mimeMessage.Body = await original.BuildMessageBodyAsync(bodyPrefix, includeAttachments, includeEmbedded).ConfigureAwait(false);

            return mimeMessage;
        }

        internal static string GetPrefixedSubject(string originalSubject, string prefix = "")
        {
            string subject = originalSubject ?? string.Empty;
            if (!string.IsNullOrEmpty(prefix) && !subject.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                subject = $"{prefix}{originalSubject}";
            return subject;
        }

        internal static InternetAddressList BuildReplyAddresses(this IMessageSummary original, bool replyToAll = false)
        {
            var to = new InternetAddressList();

            if (original.Envelope.ReplyTo.Count > 0)
                to.AddRange(original.Envelope.ReplyTo);
            else if (original.Envelope.From.Count > 0)
                to.AddRange(original.Envelope.From);
            else if (original.Envelope.Sender != null)
                to.AddRange(original.Envelope.Sender);

            if (replyToAll)
            {
                to.AddRange(original.Envelope.To);
                to.AddRange(original.Envelope.Cc);
            }

            return to;
        }

        internal static void AddMessageIdReferences(this MimeMessage mimeMessage, IMessageSummary original)
        {
            if (!string.IsNullOrEmpty(original?.Envelope?.MessageId))
            {
                mimeMessage.InReplyTo = original.Envelope.MessageId;
                if (original.References != null)
                    foreach (var id in original.References)
                        mimeMessage.References.Add(id);
                mimeMessage.References.Add(original.Envelope.MessageId);
            }
        }

        public static MimeEntity BuildMultipart(this MimeEntity mimeBody, IEnumerable<MimeEntity> mimeEntities)
        {
            if (mimeEntities == null)
                return mimeBody;

            var multipart = new Multipart();
            if (mimeBody != null)
                multipart.Add(mimeBody);
            foreach (var mimeEntity in mimeEntities)
                if (mimeEntity != null)
                    multipart.Add(mimeEntity);

            return multipart;
        }

        internal static async Task<MimeEntity> BuildMessageBodyAsync(this IMessageSummary original, string prependText = "", bool includeAttachments = true, bool includeEmbedded = true, bool setHtml = true, CancellationToken cancellationToken = default)
        {
            if (original == null)
                return new TextPart();

            bool peekFolder = !original.Folder?.IsOpen ?? false;
            if (peekFolder)
                await original.Folder.OpenAsync(FolderAccess.ReadOnly, cancellationToken);
            bool isHtml = setHtml || original.HtmlBody != null;
            var bodyText = await original.GetBodyTextAsync(cancellationToken).ConfigureAwait(false);
            var replyText = original.QuoteForReply(bodyText, prependText);
            var format = isHtml ? TextFormat.Html : TextFormat.Plain;
            var textPart = new TextPart(format) { Text = replyText };
            var mimeAttachments = await original.GetAttachmentsAsync(includeAttachments, includeEmbedded, cancellationToken).ConfigureAwait(false);
            var multipartMimeBody = textPart.BuildMultipart(mimeAttachments);
            if (peekFolder)
                await original.Folder.CloseAsync(false, cancellationToken);

            return multipartMimeBody;
        }

        public static IEnumerable<string> GetNames(this IEnumerable<BodyPartBasic> mimeEntities) =>
            mimeEntities?.Select(a => a?.FileName ?? string.Empty) ?? Array.Empty<string>();

        /// <summary>
        /// Quote the original message and add a new message above it.
        /// var bodyText = await original.GetBodyTextAsync();
        /// </summary>
        /// <param name="original"><see cref="IMessageSummary"/> to quote.</param>
        /// <param name="bodyText">TextPart of the message body.</param>
        /// <param name="message">New message to add above it.</param>
        /// <param name="includeMessageId">Include Message-ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Quoted message text.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        internal static string QuoteForReply(this IMessageSummary original, string bodyText, string message, bool includeMessageId = false, CancellationToken cancellationToken = default)
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
                    stringBuilder.AppendLine($"> Message-ID: <{original.Envelope.MessageId}>");
                stringBuilder.AppendLine($"> Sent: {original.Envelope.Date}");
                stringBuilder.AppendLine($"> From: {original.Envelope.From}");
                stringBuilder.AppendLine($"> To: {original.Envelope.To}");
                if (original.Envelope.Cc.Count > 0)
                    stringBuilder.AppendLine($"> Cc: {original.Envelope.Cc}");
                stringBuilder.AppendLine($"> Subject: {original.Envelope.Subject}");
                var attachmentCount = original.Attachments?.Count() ?? 0;
                if (attachmentCount > 0)
                {
                    var pluraliser = attachmentCount == 1 ? "" : "s";
                    var attachmentNames = original.Attachments.GetNames().ToEnumeratedString("', '");
                    stringBuilder.AppendLine($"> {attachmentCount} Attachment{pluraliser}: '{attachmentNames}'");
                }
                stringBuilder.AppendLine("> ");
                if (!string.IsNullOrEmpty(bodyText))
                {
                    string nextLine;
                    using (var reader = new StringReader(bodyText))
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
                    stringBuilder.AppendLine($"<b>Message-ID:</b> &lt;{original.Envelope.MessageId}&gt;<br />");
                stringBuilder.AppendLine($"<b>Sent:</b> {original.Envelope.Date}<br />");
                stringBuilder.AppendLine($"<b>From:</b> {GetHtml(original.Envelope.From)}<br />");
                stringBuilder.AppendLine($"<b>To:</b> {GetHtml(original.Envelope.To)}<br />");
                if (original.Envelope.Cc.Count > 0)
                    stringBuilder.AppendLine($"<b>Cc:</b> {GetHtml(original.Envelope.Cc)}<br />");
                stringBuilder.AppendLine($"<b>Subject:</b> {original.Envelope.Subject}<br />");
                var attachmentCount = original.Attachments?.Count() ?? 0;
                if (attachmentCount > 0)
                {
                    var pluraliser = attachmentCount == 1 ? "" : "s";
                    var attachmentNames = original.Attachments.GetNames().ToEnumeratedString("', '");
                    stringBuilder.AppendLine($"<b>{attachmentCount} Attachment{pluraliser}:</b> '{attachmentNames}'<br />");
                }
                stringBuilder.AppendLine("</div><br />");
                stringBuilder.AppendLine(bodyText ?? string.Empty);
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
            bool peekFolder = !messageSummary.Folder?.IsOpen ?? false;
            if (peekFolder)
                await messageSummary.Folder.OpenAsync(FolderAccess.ReadOnly, cancellationToken);
            if (messageSummary?.HtmlBody != null)
                textEntity = await messageSummary.Folder.GetBodyPartAsync(messageSummary.UniqueId, messageSummary.HtmlBody, cancellationToken);
            else if (messageSummary?.TextBody != null)
                textEntity = await messageSummary.Folder.GetBodyPartAsync(messageSummary.UniqueId, messageSummary.TextBody, cancellationToken);
            if (peekFolder)
                await messageSummary.Folder.CloseAsync(false, cancellationToken);
            var bodyText = (textEntity as TextPart)?.Text;
            return bodyText ?? string.Empty;
        }

        /// <summary>
        /// Downloads detials for all attachments linked to this message.
        /// </summary>
        /// <param name="messageSummary"><see cref="IMessageSummary"/> attachments to download.</param>
        /// <param name="includeAttachments">Only download attachments.</param>
        /// <param name="includeEmbedded">Only download non-text media type filter.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Message body parts filtered down to just the attachments.</returns>
        public static async Task<IList<MimeEntity>> GetAttachmentsAsync(this IMessageSummary messageSummary, bool includeEmbedded = true, bool includeAttachments = false, CancellationToken cancellationToken = default)
        {
            IList<MimeEntity> mimeAttachments = null;
            if (messageSummary?.Body is BodyPartMultipart multipart && (includeEmbedded || includeAttachments))
            {
                var attachments = multipart.BodyParts.OfType<BodyPartBasic>().Where(a => (includeAttachments && a.IsAttachment) ||
                    (includeEmbedded && !a.IsAttachment && !a.ContentType.MediaType.ToLower().Contains("text")));
                if (attachments.Any())
                {
                    bool peekFolder = !messageSummary.Folder?.IsOpen ?? false;
                    if (peekFolder)
                        await messageSummary.Folder.OpenAsync(FolderAccess.ReadOnly, cancellationToken);
                    mimeAttachments = new List<MimeEntity>();
                    foreach (var attachment in attachments)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        var mimeEntity = await messageSummary.Folder.GetBodyPartAsync(messageSummary.UniqueId, attachment, cancellationToken).ConfigureAwait(false);
                        mimeAttachments.Add(mimeEntity);
                    }
                    if (peekFolder)
                        await messageSummary.Folder.CloseAsync(false, cancellationToken);
                }
            }
            return mimeAttachments;
        }
    }
}
