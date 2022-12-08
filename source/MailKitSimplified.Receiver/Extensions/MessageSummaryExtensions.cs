using MimeKit;
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
        public static IEnumerable<string> GetAttachmentNames(this IEnumerable<BodyPartBasic> mimeEntities) =>
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
        internal static string QuoteForReply(this IMessageSummary original, string bodyText, string message = "", bool includeMessageId = false, CancellationToken cancellationToken = default)
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
                if (original.Attachments?.Any() ?? false)
                {
                    var attachmentCount = original.Attachments.Count();
                    var pluraliser = attachmentCount == 1 ? "" : "s";
                    var attachments = original.GetMailAttachmentDetails();
                    var attachmentNames = attachments.GetAttachmentNames().ToEnumeratedString("', '");
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
                if (original.Attachments?.Any() ?? false)
                {
                    var attachmentCount = original.Attachments.Count();
                    var pluraliser = attachmentCount == 1 ? "" : "s";
                    var attachments = original.GetMailAttachmentDetails();
                    var attachmentNames = attachments.GetAttachmentNames().ToEnumeratedString("', '");
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
