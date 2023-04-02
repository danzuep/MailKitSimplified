using MimeKit;
using System;
using System.Linq;
using System.Collections.Generic;
using CommunityToolkit.Common;
using CommunityToolkit.Diagnostics;
using MailKitSimplified.Receiver.Extensions;
using EmailWpfApp.Models;

namespace EmailWpfApp.Extensions
{
    public static class EmailConverter
    {
        public static Email Convert(this MimeMessage mimeMessage)
        {
            Guard.IsNotNull(mimeMessage, nameof(mimeMessage));
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
            email.BodyText = mimeMessage.TextBody?.Trim() ??
                mimeMessage.HtmlBody?.DecodeHtml() ?? string.Empty;
            email.BodyHtml = mimeMessage.HtmlBody?.Trim() ?? email.BodyText;
            return email;
        }

        public static IEnumerable<Email> Convert<TIn>(this IEnumerable<TIn>? values) where TIn : MimeMessage =>
            values?.Select(c => c?.Convert()!).Where(c => c != null) ?? Array.Empty<Email>();

        //public static Email Convert(this IMessageSummary messageSummary)
        //{
        //    var email = new Email();
        //    email.MessageId = messageSummary.MessageId;
        //    email.Date = messageSummary.Date.ToString();
        //    email.From = messageSummary.From.ToString();
        //    email.To = messageSummary.To.ToString();
        //    email.Cc = messageSummary.Cc.ToString();
        //    email.Bcc = messageSummary.Bcc.ToString();
        //    email.Headers = messageSummary.Headers.ToEnumeratedString();
        //    email.Attachments = messageSummary.Attachments?.Count().ToString() ?? "0";
        //    email.Subject = messageSummary.Subject ?? string.Empty;
        //    email.BodyText = messageSummary.TextBody?.Trim() ?? string.Empty;
        //    email.BodyHtml = messageSummary.HtmlBody?.Trim() ?? email.BodyText;
        //    return email;
        //}
    }
}
