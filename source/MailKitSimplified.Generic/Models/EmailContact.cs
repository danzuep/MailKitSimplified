using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging;
using MailKitSimplified.Generic.Abstractions;

namespace MailKitSimplified.Generic.Models
{
    public class EmailContact : IEmailContact
    {
        public string Name { get; set; }

        [Required(ErrorMessage = "Email address is required")]
        [DataType(DataType.EmailAddress)]
        public string EmailAddress { get; set; }

        private static readonly char[] _emailReplace = new char[] { '_', '.', '-' };
        private static readonly char[] _emailSeparator = new char[] { ';', ',', ' ', '&', '|' };

        private EmailContact(string emailAddress, string name = null)
        {
            EmailAddress = emailAddress ?? throw new ArgumentNullException(nameof(emailAddress));
            bool hasNoName = string.IsNullOrWhiteSpace(name) ||
                name.Equals(emailAddress, StringComparison.OrdinalIgnoreCase);
            Name = hasNoName ? GetNameFromEmailAddress(emailAddress) : name;
        }

        public static IEmailContact Create(string emailAddress, string name = null) =>
            new EmailContact(emailAddress, name);

        private static string GetNameFromEmailAddress(string emailAddress)
        {
            var email = emailAddress?.Split('@')?.FirstOrDefault();
            string name = SpaceReplaceTitleCase(email, _emailReplace) ?? string.Empty;
            return name;
        }

        protected static IEmailContact GetContactFromEmailAddress(string emailAddress)
        {
            string name = GetNameFromEmailAddress(emailAddress);
            var contact = Create(emailAddress, name);
            return contact;
        }

        public static IEnumerable<IEmailContact> ParseEmailContacts(string value)
        {
            IEnumerable<IEmailContact> contacts = null;
            if (!string.IsNullOrEmpty(value))
            {
                var emailAddresses = value.Split(_emailSeparator, StringSplitOptions.RemoveEmptyEntries);
                contacts = emailAddresses.Select(email => GetContactFromEmailAddress(email));
            }
            return contacts ?? Enumerable.Empty<IEmailContact>();
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
                    bool isNextLower = nextChar != null &&
                        char.IsLower(nextChar.Value);
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

        public static bool ValidateEmailAddresses(IEnumerable<string> sourceEmailAddresses, IEnumerable<string> destinationEmailAddresses, ILogger logger)
        {
            if (sourceEmailAddresses is null)
                throw new ArgumentNullException(nameof(sourceEmailAddresses));
            if (destinationEmailAddresses is null)
                throw new ArgumentNullException(nameof(destinationEmailAddresses));
            if (logger is null)
                logger = NullLogger<EmailContact>.Instance;
            bool isValid = true;
            int sourceEmailAddressCount = 0, destinationEmailAddressCount = 0;
            foreach (var from in sourceEmailAddresses)
            {
                if (!from.Contains("@"))
                {
                    logger.LogWarning($"From address is invalid ({from})");
                    isValid = false;
                }
                foreach (var to in destinationEmailAddresses)
                {
                    if (!to.Contains("@"))
                    {
                        logger.LogWarning($"To address is invalid ({to})");
                        isValid = false;
                    }
                    if (to.Equals(from, StringComparison.OrdinalIgnoreCase))
                    {
                        logger.LogWarning($"Circular reference, To ({to}) == From ({from})");
                        isValid = false;
                    }
                    destinationEmailAddressCount++;
                }
                sourceEmailAddressCount++;
            }
            if (sourceEmailAddressCount == 0)
                logger.LogWarning("Source email address not specified");
            if (destinationEmailAddressCount == 0)
                logger.LogWarning("Destination email address not specified");
            isValid &= sourceEmailAddressCount > 0 && destinationEmailAddressCount > 0;
            return isValid;
        }

        public override string ToString() => $"\"{Name}\" <{EmailAddress}>";
    }
}
