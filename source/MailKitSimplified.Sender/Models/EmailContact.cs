using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MailKitSimplified.Sender.Models
{
    public class EmailContact
    {
        public string Name { get; set; }

        [Required(ErrorMessage = "Email address is required")]
        [DataType(DataType.EmailAddress)]
        public string Address { get; set; }

        private static readonly char[] _emailReplace = new char[] { '_', '.', '-' };
        private static readonly char[] _emailSeparator = new char[] { ';', ',', ' ', '&', '|' };

        public EmailContact(string emailAddress, string name = null)
        {
            Address = emailAddress ?? throw new ArgumentNullException(nameof(emailAddress));
            bool hasNoName = string.IsNullOrWhiteSpace(name) ||
                name.Equals(emailAddress, StringComparison.OrdinalIgnoreCase);
            Name = hasNoName ? GetNameFromEmailAddress(emailAddress) : name;
        }

        private static string GetNameFromEmailAddress(string emailAddress)
        {
            var email = emailAddress?.Split('@')?.FirstOrDefault();
            string name = SpaceReplaceTitleCase(email, _emailReplace) ?? string.Empty;
            return name;
        }

        protected static EmailContact GetContactFromEmailAddress(string emailAddress)
        {
            string name = GetNameFromEmailAddress(emailAddress);
            var contact = new EmailContact(name, emailAddress);
            return contact;
        }

        public static IEnumerable<EmailContact> ParseEmailContacts(string value)
        {
            IEnumerable<EmailContact> contacts = null;
            if (!string.IsNullOrEmpty(value))
            {
                var emailAddresses = value.Split(_emailSeparator, StringSplitOptions.RemoveEmptyEntries);
                contacts = emailAddresses.Select(email => GetContactFromEmailAddress(email));
            }
            return contacts ?? Enumerable.Empty<EmailContact>();
        }

        private static string SpaceReplaceTitleCase(string value, char[] replace)
        {
            string result = Replace(value, replace, ' ');

            if (result.Length > 0)
            {
                var builder = new List<char>();
                char[] array = result.ToCharArray();
                char prevChar = array[0];
                builder.Add(char.ToUpper(prevChar));

                for (int i = 1; i < array.Length; i++)
                {
                    char thisChar = array[i];
                    char? nextChar = i + 1 < array.Length ?
                        array[i + 1] : null as char?;
                    bool isNextLower = nextChar == null ?
                        false : char.IsLower(nextChar.Value);
                    bool isPrevUpper = char.IsUpper(prevChar);
                    bool isPrevLower = char.IsLower(prevChar);
                    bool isThisUpper = char.IsUpper(thisChar);
                    bool isAcronym = isThisUpper && isPrevUpper;
                    bool isTitleCase = isAcronym && isNextLower;
                    bool isWordEnd = isThisUpper && isPrevLower;
                    if (isWordEnd || isTitleCase)
                    {
                        builder.Add(' ');
                    }
                    builder.Add(thisChar);
                    prevChar = thisChar;
                }

                result = new string(builder.ToArray());
            }

            return result;
        }

        private static string Replace(string value, char[] oldChars, char newChar)
        {
            string result = value ?? string.Empty;
            if (oldChars != null)
                foreach (var r in oldChars)
                    result = result.Replace(r, newChar);
            return result;
        }

        public override string ToString() => $"{Name} <{Address}>";
    }
}
