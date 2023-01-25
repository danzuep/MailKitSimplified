using MimeKit;
using System;
using System.Linq;
using System.Collections.Generic;
using EmailWpfApp.Models;
using MailKitSimplified.Receiver.Extensions;

namespace EmailWpfApp.Extensions
{
    public static class EmailConverter
    {
        public static Email Convert(this MimeMessage mimeMessage)
        {
            var email = new Email();
            email.MessageId = mimeMessage.MessageId;
            email.Date = mimeMessage.Date.ToString();
            email.From = mimeMessage.From.ToString();
            email.To = mimeMessage.To.ToString();
            email.Cc = mimeMessage.Cc.ToString();
            email.Bcc = mimeMessage.Bcc.ToString();
            email.Headers = mimeMessage.Headers.ToEnumeratedString();
            email.Attachments = mimeMessage.Attachments?.Count().ToString() ?? "0";
            email.Subject = mimeMessage.Subject ?? string.Empty;
            email.BodyText = mimeMessage.TextBody?.Trim() ?? string.Empty;
            email.BodyHtml = mimeMessage.HtmlBody?.Trim() ?? email.BodyText;
            return email;
        }

        public static IEnumerable<Email> Convert<TIn>(this IEnumerable<TIn>? values) where TIn : MimeMessage =>
            values?.Select(c => c?.Convert()!).Where(c => c != null) ?? Array.Empty<Email>();
    }
}
