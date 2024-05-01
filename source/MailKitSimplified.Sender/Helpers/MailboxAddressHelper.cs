﻿using MimeKit;
using System;
using System.Linq;
using System.Collections.Generic;

namespace MailKitSimplified.Sender.Helpers
{
    public static class MailboxAddressHelper
    {
        private static readonly char[] _emailReplace = new char[] { '_', '.', '-' };
        private static readonly char[] _emailSeparator = new char[] { ';', ',', ' ', '&', '|' };

#if NET5_0_OR_GREATER
        /// <summary>
        /// Truncated hexadecimal string with up to 16^32 combinations, used for logging.
        /// </summary>
        /// <param name="count">Base 16 exponent.</param>
        /// <returns>Truncated hexadecimal string</returns>
        public static string GenerateHexId(byte count = 32)
        {
            if (count > 32)
                count = 32;
            var uuid = Guid.NewGuid().ToString("n", null)[..count];
            return uuid;
        }
#endif

        public static IEnumerable<MailboxAddress> ParseEmailContacts(string value)
        {
            IEnumerable<MailboxAddress> contacts = null;
            if (!string.IsNullOrEmpty(value))
            {
                var emailAddresses = value.Split(_emailSeparator, StringSplitOptions.RemoveEmptyEntries);
                contacts = emailAddresses.Select(email => GetContactFromEmailAddress(email));
            }
            return contacts ?? Enumerable.Empty<MailboxAddress>();
        }

        public static MailboxAddress GetContactFromEmailAddress(string emailAddress)
        {
            string name = GetNameFromEmailAddress(emailAddress);
            var contact = new MailboxAddress(name, emailAddress);
            return contact;
        }

        public static MailboxAddress GenerateGuidIfFromNotSet(string exampleEmailAddress)
        {
            string domain = string.IsNullOrEmpty(exampleEmailAddress) ?
                "localhost" : GetDomainFromEmailAddress(exampleEmailAddress);
            var contact = new MailboxAddress(string.Empty, $"{Guid.NewGuid():N}@{domain}");
            return contact;
        }

        internal static string GetNameFromEmailAddress(string emailAddress)
        {
            var email = emailAddress?.Split('@')?.FirstOrDefault();
            string name = SpaceReplaceTitleCase(email, _emailReplace) ?? string.Empty;
            return name;
        }

        internal static string GetDomainFromEmailAddress(string emailAddress)
        {
            if (emailAddress?.IndexOf('@') is int index && index >= 0)
                return emailAddress.Substring(index + 1);
            return emailAddress;
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
                    bool isPrevSpace = prevChar == ' ';
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
                    else if (isPrevSpace && !isThisUpper)
                    {
                        thisChar = char.ToUpper(thisChar);
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
    }
}
