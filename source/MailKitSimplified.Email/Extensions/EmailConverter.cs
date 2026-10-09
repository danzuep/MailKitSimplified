using MimeKit;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Common;
using CommunityToolkit.Diagnostics;
using MailKitSimplified.Receiver.Extensions;
using MailKitSimplified.Generic.Abstractions;
using MailKitSimplified.Generic.Models;
using MailKitSimplified.Generic.Services;

namespace MailKitSimplified.Email.Extensions
{
    public static class EmailConverter
    {
        /// <summary>Materializes a generic email and its composed body for MailKit.</summary>
        /// <param name="email">The generic email.</param>
        /// <param name="cancellationToken">Cancels attachment materialization.</param>
        /// <returns>A MIME message owned by the caller, who must dispose it after use.</returns>
        public static async Task<MimeMessage> ToMimeMessageAsync(this IGenericEmail email,
            CancellationToken cancellationToken = default)
        {
            if (email == null)
                throw new ArgumentNullException(nameof(email));
            cancellationToken.ThrowIfCancellationRequested();
            var message = new MimeMessage();
            try
            {
                foreach (var header in email.Headers)
                    message.Headers.Add(header.Key, header.Value);
                message.From.AddRange(email.From.Select(contact => new MailboxAddress(contact.Name, contact.EmailAddress)));
                message.ReplyTo.AddRange(email.ReplyTo.Select(contact => new MailboxAddress(contact.Name, contact.EmailAddress)));
                message.To.AddRange(email.To.Select(contact => new MailboxAddress(contact.Name, contact.EmailAddress)));
                message.Cc.AddRange(email.Cc.Select(contact => new MailboxAddress(contact.Name, contact.EmailAddress)));
                message.Bcc.AddRange(email.Bcc.Select(contact => new MailboxAddress(contact.Name, contact.EmailAddress)));
                message.Subject = email.Subject ?? string.Empty;

                if (email is Generic.Models.GenericEmail composed && composed.Body != null)
                {
                    EmailAstValidator.Validate(composed.Body);
                    message.Body = await MapBodyAsync(composed.Body, cancellationToken).ConfigureAwait(false);
                }
                else
                    message.Body = new BodyBuilder { TextBody = email.BodyText, HtmlBody = email.BodyHtml }.ToMessageBody();

                foreach (var attachment in email.Attachments)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    MimeEntity part;
                    if (attachment.Value is EmailAttachment typed)
                        part = await MapAttachmentAsync(typed, cancellationToken).ConfigureAwait(false);
                    else
                    {
                        var legacy = new BodyBuilder();
                        if (attachment.Value is byte[] bytes)
                            part = legacy.Attachments.Add(attachment.Key, bytes);
                        else if (attachment.Value is Stream stream)
                            part = await legacy.Attachments.AddAsync(attachment.Key, stream, cancellationToken).ConfigureAwait(false);
                        else if (attachment.Value == null)
                            part = await legacy.Attachments.AddAsync(attachment.Key, cancellationToken).ConfigureAwait(false);
                        else if (attachment.Value is MimeEntity entity)
                        {
                            using (var buffer = new MemoryStream())
                            {
                                await entity.WriteToAsync(buffer, cancellationToken).ConfigureAwait(false);
                                buffer.Position = 0;
                                part = await MimeEntity.LoadAsync(buffer, cancellationToken).ConfigureAwait(false);
                            }
                        }
                        else
                            throw new NotSupportedException("The attachment content type is not supported.");
                    }
                    var mixed = message.Body as Multipart;
                    if (mixed == null || !mixed.ContentType.IsMimeType("multipart", "mixed"))
                    {
                        mixed = new Multipart("mixed");
                        if (message.Body != null)
                            mixed.Add(message.Body);
                        message.Body = mixed;
                    }
                    mixed.Add(part);
                }
                return message;
            }
            catch
            {
                message.Dispose();
                throw;
            }
        }

        private static async Task<MimeEntity> MapBodyAsync(BodyNode node, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (node is TextBodyNode text)
                return new TextPart("plain") { Text = text.Text };
            if (node is HtmlBodyNode html)
                return new TextPart("html") { Text = html.Html };
            if (node is AttachmentNode attachment)
                return await MapAttachmentAsync(attachment.Attachment, cancellationToken).ConfigureAwait(false);
            if (node is MultipartContainerNode container)
            {
                var multipart = container.Kind == MultipartKind.Alternative ? (Multipart)new MultipartAlternative() :
                    container.Kind == MultipartKind.Related ? new MultipartRelated() : new Multipart("mixed");
                try
                {
                    foreach (var child in container.Children)
                        multipart.Add(await MapBodyAsync(child, cancellationToken).ConfigureAwait(false));
                    return multipart;
                }
                catch
                {
                    multipart.Dispose();
                    throw;
                }
            }
            throw new NotSupportedException("The composed body type is not supported.");
        }

        private static async Task<MimeEntity> MapAttachmentAsync(EmailAttachment attachment,
            CancellationToken cancellationToken)
        {
            var part = new MimePart(ContentType.Parse(attachment.ContentType))
            {
                ContentDisposition = new ContentDisposition(attachment.IsInline ? "inline" : "attachment"),
                FileName = attachment.FileName,
                ContentTransferEncoding = ContentEncoding.Base64
            };
            try
            {
                if (attachment.ContentId != null)
                    part.ContentId = attachment.ContentId;
                part.Content = new MimeContent(await attachment.OpenReadAsync(cancellationToken).ConfigureAwait(false));
                return part;
            }
            catch
            {
                part.Dispose();
                throw;
            }
        }

        public static Models.Email Convert(this MimeMessage mimeMessage)
        {
            Guard.IsNotNull(mimeMessage, nameof(mimeMessage));
            var email = new Models.Email
            {
                MessageId = mimeMessage.MessageId,
                Date = mimeMessage.Date.ToString("R"),
                From = mimeMessage.From.ToString(),
                To = mimeMessage.To.ToString(),
                Cc = mimeMessage.Cc.ToString(),
                Bcc = mimeMessage.Bcc.ToString(),
                Headers = mimeMessage.Headers.ToEnumeratedString(),
                AttachmentCount = mimeMessage.Attachments?.Count() ?? 0,
                AttachmentNames = mimeMessage.Attachments.GetAttachmentNames(),
                Subject = mimeMessage.Subject ?? string.Empty,
                BodyText = mimeMessage.TextBody?.Trim() ??
                    mimeMessage.HtmlBody?.DecodeHtml() ?? string.Empty
            };
            email.BodyHtml = mimeMessage.HtmlBody?.Trim() ?? email.BodyText;
            return email;
        }

        public static IEnumerable<Models.Email> Convert<TIn>(this IEnumerable<TIn> values) where TIn : MimeMessage => /*where TOut : Models.Email*/
            values?.Select(c => c?.Convert()).Where(c => c != null) ?? Array.Empty<Models.Email>();
    }
}
