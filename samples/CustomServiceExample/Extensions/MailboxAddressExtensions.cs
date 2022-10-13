using CustomServiceExample.Extensions;
using MimeKit;

namespace MailKit.Simple.Core.Extensions
{
    public static class MailboxAddressExtensions
    {
        //private static bool IsSeparator(this char? c) =>
        //    c != null && c.Value.IsSeparator();

        //private static bool IsSeparator(this char c) =>
        //    char.IsWhiteSpace(c) ||
        //    char.IsPunctuation(c) ||
        //    char.IsSeparator(c);

        internal static string ToSpaceReplaceTitleCase(
            this string value, char[]? replace = null)
        {
            string result = value.Replace(replace, ' ');

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

        public static string Replace(
            this string value, char[] oldChars, char newChar)
        {
            string result = value?.Clone() as string ?? "";
            if (oldChars != null)
                foreach (var r in oldChars)
                    result = result.Replace(r, newChar);
            return result;
        }

        public static MailboxAddress FormatMailboxName(
            this MailboxAddress contact, char[] replace = null)
        {
            if (replace == null)
                replace = new char[] { '_', '.', '-' };

            bool noName = string.IsNullOrWhiteSpace(contact.Name) ||
                contact.Name.Equals(contact.Address, StringComparison.OrdinalIgnoreCase) ? true : false;
            string name = noName ? contact.Address?.Split('@')?.First()
                .ToSpaceReplaceTitleCase(replace) ?? contact.Name : contact.Name;

            return new MailboxAddress(name, contact.Address);
        }

        public static IEnumerable<MailboxAddress> FormatMailboxName(
            this IEnumerable<MailboxAddress> addresses)
        {
            var results = new List<MailboxAddress>();
            if (addresses != null && addresses.Any())
            {
                var replace = new char[] { '_', '.', '-' };
                foreach (var a in addresses)
                {
                    results.Add(a.FormatMailboxName(replace));
                }
            }
            return results;
        }

        public static string FormatMailboxNameAddress(
            this InternetAddressList internetAddresses) =>
                internetAddresses?.Mailboxes.FormatMailboxName()
                    .Select(m => $"\"{m.Name}\" <{m.Address}>").ToEnumeratedString("; ") ?? "";
    }
}
