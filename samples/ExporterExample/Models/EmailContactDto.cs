using System.ComponentModel.DataAnnotations;
using ExporterExample.Abstractions;

namespace ExporterExample.Models
{
    public class EmailContactDto : IEmailAddressDto
    {
        public string Name { get; set; }

        [Required(ErrorMessage = "Email address is required")]
        [DataType(DataType.EmailAddress)]
        public string Email { get; set; }

        public EmailContactDto(string emailAddress, string? name = null)
        {
            Email = emailAddress ?? throw new ArgumentNullException(nameof(emailAddress));
            Name = name ?? "";
        }

        public static IEmailAddressDto Create(string emailAddress, string? name = null) =>
            new EmailContactDto(emailAddress, name);

        public override string ToString() => $"{Name} <{Email}>";
    }
}
