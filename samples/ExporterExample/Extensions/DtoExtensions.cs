using ExporterExample.Abstractions;
using ExporterExample.Models;
using MailKit;

namespace ExporterExample.Extensions
{
    internal static class DtoExtensions
    {
        public static IEmailDto ToDto(this IMessageSummary messageSummary)
        {
            if (messageSummary.Envelope is not Envelope envelope)
            {
                return new EmailDto();
            }
            var emailDto = new EmailDto
            {
                Date = envelope.Date,
                From = envelope.From.Mailboxes.Select(m => m.ToString()),
                To = envelope.To.Mailboxes.Select(m => m.ToString()),
                Cc = envelope.Cc.Mailboxes.Select(m => m.ToString()),
                Bcc = envelope.Bcc.Mailboxes.Select(m => m.ToString()),
                MessageId = envelope.MessageId ?? string.Empty,
                Subject = envelope.Subject ?? string.Empty
            };
            return emailDto;
        }
    }
}
