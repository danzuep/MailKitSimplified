using MimeKit;
using MailKit;
using MailKit.Search;
using System;
using System.Text;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net.Mail;
using static System.Net.Mime.MediaTypeNames;
using System.IO;
using System.Reflection;

namespace MailKitSimplified.Receiver.Extensions
{
    [ExcludeFromCodeCoverage]
    public static class EmailReceiverExtensions
    {
        public static string ToEnumeratedString<T>(this IEnumerable<T> data, string div = ", ") =>
            data is null ? "" : string.Join(div, data.Select(o => o?.ToString() ?? ""));

        public static IList<T> TryAddUniqueRange<T>(this IList<T> list, IEnumerable<T> items)
        {
            var result = new List<T>();
            if (list is null)
                list = new List<T>();
            if (items != null)
            {
                foreach (T item in items)
                {
                    if (item != null && !list.Contains(item))
                    {
                        list.Add(item);
                        result.Add(item);
                    }
                }
            }
            return result;
        }

        public static void ActionEach<T>(this IEnumerable<T> items, Action<T> action, CancellationToken cancellationToken = default)
        {
            if (items != null && action != null)
            {
                foreach (T item in items)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;
                    else if (item != null)
                        action(item);
                }
            }
        }

        /// <summary>
        /// Downloads the text/html part of the body if it exists, otherwise downloads the text/plain part.
        /// </summary>
        /// <param name="mail"><see cref="IMessageSummary"/> body to download.</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns><see cref="TextPart"/> of the message body <see cref="MimeEntity"/>.</returns>
        public static async Task<string> GetBodyTextAsync(this IMessageSummary mail, CancellationToken cancellationToken = default)
        {
            MimeEntity textEntity = null;
            bool peekFolder = !mail?.Folder?.IsOpen ?? false;
            if (peekFolder)
                await mail.Folder.OpenAsync(FolderAccess.ReadOnly, cancellationToken);
            if (mail?.HtmlBody != null)
                textEntity = await mail.Folder.GetBodyPartAsync(mail.UniqueId, mail.HtmlBody, cancellationToken);
            else if (mail?.TextBody != null)
                textEntity = await mail.Folder.GetBodyPartAsync(mail.UniqueId, mail.TextBody, cancellationToken);
            if (peekFolder)
                await mail.Folder.CloseAsync(false, cancellationToken);
            var textPart = textEntity as TextPart;
            return textPart?.Text ?? string.Empty;
        }

        /// <summary>
        /// Quote the original message and add a new message above it.
        /// </summary>
        /// <param name="original"><see cref="MimeMessage"/> to quote.</param>
        /// <param name="message">New message to add above it.</param>
        /// <param name="htmlBorder">HTML border style.</param>
        /// <returns>Quoted message text.</returns>
        public static string QuoteForReply(this MimeMessage original, string message = "")
        {
            if (original == null)
                throw new ArgumentNullException(nameof(original));

            var stringBuilder = new StringBuilder();
            if (original.HtmlBody == null)
            {
                stringBuilder.AppendLine(message);
                stringBuilder.AppendLine();
                stringBuilder.AppendLine("---------- Original Message ----------");
                stringBuilder.AppendLine($"> From: {original.From}");
                stringBuilder.AppendLine($"> Sent: {original.Date}");
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
                stringBuilder.AppendLine($"> Message ID: {original.MessageId}");
                if (original.ResentFrom.Count > 0)
                    stringBuilder.AppendLine($"> Resent From: {original.ResentFrom}");
                stringBuilder.AppendLine("> ");
                if (!string.IsNullOrEmpty(original.TextBody))
                {
                    string nextLine;
                    using (var reader = new StringReader(original.TextBody))
                    {
                        while ((nextLine = reader.ReadLine()) != null)
                        {
                            stringBuilder.Append("> ");
                            stringBuilder.AppendLine(nextLine);
                        }
                    }
                    stringBuilder.AppendLine();
                }
            }
            else
            {
                string GetHtml(InternetAddressList contacts) =>
                    contacts.Mailboxes.Select(a => $"\"{a.Name}\" &lt{a.Address}&gt").ToEnumeratedString("; ");
                stringBuilder.AppendLine("<div>");
                stringBuilder.AppendLine(message);
                stringBuilder.AppendLine("</div><br /><blockquote><hr /><div>");
                stringBuilder.AppendLine($"<b>From:</b> {GetHtml(original.From)}<br />");
                stringBuilder.AppendLine($"<b>Sent:</b> {original.Date}<br />");
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
                stringBuilder.AppendLine($"<b>Message ID:</b> {original.MessageId}<br />");
                if (original.ResentFrom.Count > 0)
                    stringBuilder.AppendLine($"<b>Resent From:</b> {GetHtml(original.ResentFrom)}<br />");
                stringBuilder.AppendLine("</div><br />");
                stringBuilder.AppendLine(original.HtmlBody ?? string.Empty);
                stringBuilder.AppendLine("</blockquote>");
            }
            var result = stringBuilder.ToString();
            return result;
        }
    }
}
