using System.ComponentModel.DataAnnotations;

namespace MailKitSimplified.Sender.Models
{
    public class EmailContact
    {
        public string Name { get; set; }

        [Required(ErrorMessage = "Email address is required")]
        [DataType(DataType.EmailAddress)]
        public string Address { get; set; }

        public EmailContact(string address, string name = "")
        {
            Address = address;
            Name = name;
        }

        public override string ToString() => $"{Name} <{Address}>";
    }
}
