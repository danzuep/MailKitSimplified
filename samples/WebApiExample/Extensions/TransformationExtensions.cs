using MimeKit;
using System.Diagnostics.CodeAnalysis;
using WebApiExample.Models;

namespace WebApiExample.Extensions
{
    internal static class TransformationExtensions
    {
        public static EmailDto ToDto([NotNull] this MimeMessage value)
        {
            return new EmailDto()
            {
                From = value.From.Mailboxes.Select(m =>
                    EmailContactDto.Create(m.Address, m.Name)),
                To = value.To.Mailboxes.Select(m =>
                    EmailContactDto.Create(m.Address, m.Name)),
                Subject = value.Subject ?? string.Empty,
                BodyText = value.TextBody ?? string.Empty
            };
        }
    }
}
