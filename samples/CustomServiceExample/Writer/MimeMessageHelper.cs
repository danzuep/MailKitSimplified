using MimeKit;
using MimeKit.Text;
using MailKit.Simple.Core.Extensions;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using System.Linq;
using System.IO;
using System.Threading;
using System.Diagnostics;
using System.Text;
using CustomServiceExample.Extensions;

namespace CustomServiceExample.Writer
{
    public static class EmailHelper
    {
        public static MimeMessage CreateMimeMessage(
            string from, string to, string subject, string bodyText,
            bool isHtml = true, string replyTo = "", IEnumerable<MimeEntity>? attachments = null)
        {
            CreateMimeEnvelope(from, to, bodyText, isHtml, replyTo,
                out IEnumerable<MailboxAddress> mFrom, out IEnumerable<MailboxAddress> mTo,
                out MimeEntity mBody, out IEnumerable<MailboxAddress> mReplyTo);
            return CreateMimeMessage(mFrom, mTo, subject, mBody, mReplyTo, attachments);
        }

        internal static void CreateMimeEnvelope(
            string from, string to, string bodyText, bool isHtml, string replyTo,
            out IEnumerable<MailboxAddress> mFrom, out IEnumerable<MailboxAddress> mTo,
            out MimeEntity mTextBody, out IEnumerable<MailboxAddress> mReplyTo)
        {
            mFrom = ParseMailboxAddress(from);
            mTo = ParseMailboxAddress(to);
            mReplyTo = ParseMailboxAddress(replyTo);
            var format = isHtml ? TextFormat.Html : TextFormat.Plain;
            mTextBody = new TextPart(format) { Text = bodyText ?? "" };
        }

        public static IEnumerable<MailboxAddress> ParseMailboxAddress(string value)
        {
            char[] replace = new char[] { '_', '.', '-' };
            char[] separator = new char[] { ';', ',', ' ', '|' };
            return string.IsNullOrEmpty(value) ? Array.Empty<MailboxAddress>() :
                value.Split(separator, StringSplitOptions.RemoveEmptyEntries)
                    .Select(f => new MailboxAddress(f.Split('@').FirstOrDefault()
                        ?.ToSpaceReplaceTitleCase(replace) ?? f, f));
        }

        internal static string ToSpaceReplaceTitleCase(
            this string value, char[] replace = null)
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

        internal static string GetPrefixedSubject(string originalSubject, string prefix = "")
        {
            string subject = originalSubject ?? "";
            if (!string.IsNullOrEmpty(prefix) &&
                !subject.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                subject = string.Join(" ", prefix, subject);
            return subject ?? "";
        }

        internal static MimeMessage CreateMimeMessage(
            IEnumerable<MailboxAddress> from, IEnumerable<MailboxAddress> to, string subject,
            MimeEntity textBody, IEnumerable<MailboxAddress> replyTo = null, IEnumerable<MimeEntity> attachments = null)
        {
            var body = textBody ?? new TextPart(TextFormat.Html);
            string attachmentNames = string.Empty;

            if (attachments != null && attachments.Any())
            {
                var multipart = new Multipart("mixed");
                multipart.Add(textBody);
                foreach (var attachment in attachments)
                    multipart.Add(attachment);
                body = multipart;
                //attachmentNames = string.Format(", with attached: '{0}'",
                //    attachments.GetAttachmentNames().ToEnumeratedString("', '"));
            }

            if (replyTo.IsNullOrEmpty())
            {
                replyTo = from;
            }
            from = from.FormatMailboxName();
            to = to.FormatMailboxName();
            replyTo = replyTo.FormatMailboxName();

            var message = new MimeMessage(from, to, subject, body);
            message.ReplyTo.AddRange(replyTo);

            return message;
        }

        public static IEnumerable<string> GetEmlFilesFromFolder(params string[] folderPath)
        {
            string path = folderPath?.Length > 1 ? Path.Combine(folderPath) :
                folderPath?.Length > 0 ? folderPath[0] : "";
            IEnumerable<string> uncFileNames = Array.Empty<string>();
            if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
            {
                uncFileNames = EnumerateFilesFromFolder(path, "*.eml", false);
                if (!uncFileNames.Any())
                    uncFileNames = EnumerateFilesFromFolder(path, "*.eml", true);
            }
            return uncFileNames;
        }

        public static IEnumerable<string> EnumerateFilesFromFolder(
            string uncPath, string extension = "*", bool searchAll = false, bool createDirectory = false)
        {
            extension = ConvertToFileExtensionFilter(extension);
            var option = searchAll ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            return CheckDirectory(uncPath, createDirectory) ?
                Directory.EnumerateFiles(uncPath, extension, option) : Array.Empty<string>();
        }

        public static string ConvertToFileExtensionFilter(string fileExtension)
        {
            var sb = new StringBuilder();
            if (string.IsNullOrEmpty(fileExtension))
                fileExtension = "*";
            else if (fileExtension.StartsWith("."))
                sb.Append("*");
            else if (!fileExtension.StartsWith("*."))
                sb.Append("*.");
            sb.Append(fileExtension);
            return sb.ToString();
        }

        public static bool CheckDirectory(string uncPath, bool createDirectory = false)
        {
            bool exists = Directory.Exists(uncPath);
            if (createDirectory)
                CreateDirectory(uncPath);
            else if (!exists)
                Trace.TraceWarning("Folder not found: '{0}'.", uncPath);
            return exists;
        }

        public static void CreateDirectory(string uncPath)
        {
            try
            {
                //var security = new DirectorySecurity("ReadFolder", AccessControlSections.Access);
                // If the directory already exists, this method does not create a new directory.
                Directory.CreateDirectory(uncPath);
            }
            catch (UnauthorizedAccessException ex)
            {
                Trace.TraceWarning("'{0}' CreateDirectory access is denied, folder not created. {1}", uncPath, ex.Message);
            }
        }

        public static IEnumerable<MimeMessage> GetMimeMessagesFromFolder(
            string folderPath, CancellationToken ct = default)
        {
            var emlFiles = GetEmlFilesFromFolder(folderPath);
            return GetMimeMessagesFromPath(emlFiles, ct);
        }

        public static IEnumerable<MimeMessage> GetMimeMessagesFromPath(
            IEnumerable<string> uncFileNames, CancellationToken ct = default)
        {
            if (uncFileNames != null)
            {
                foreach (var filePath in uncFileNames)
                {
                    if (ct.IsCancellationRequested)
                        break;
                    if (File.Exists(filePath))
                    {
                        yield return MimeMessage.Load(filePath, ct);
                    }
                }
            }
        }
    }
}
