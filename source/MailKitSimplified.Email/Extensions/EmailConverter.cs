using MimeKit;
using System;
using System.Linq;
using System.Collections.Generic;
using CommunityToolkit.Common;
using CommunityToolkit.Diagnostics;
using MailKitSimplified.Receiver.Extensions;

namespace MailKitSimplified.Email.Extensions
{
    public static class EmailConverter
    {
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
