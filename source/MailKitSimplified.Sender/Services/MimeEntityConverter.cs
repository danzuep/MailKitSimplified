using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging;
using MimeKit;
using MailKitSimplified.Sender.Models;

namespace MailKitSimplified.Sender.Services
{
    public sealed class MimeEntityConverter
    {
        private readonly ILogger _logger;

        public MimeEntityConverter(ILogger<MimeEntityConverter> logger = null)
        {
            _logger = logger ?? NullLogger<MimeEntityConverter>.Instance;
        }

        public static EmailContact FormatContactName(EmailContact contact, char[] replace = null)
        {
            if (replace == null)
                replace = new char[] { '_', '.', '-' };

            bool noName = string.IsNullOrWhiteSpace(contact.Name) ||
                contact.Name.Equals(contact.Address, StringComparison.OrdinalIgnoreCase) ? true : false;
            string name = noName ? ToSpaceReplaceTitleCase(contact.Address?.Split('@')?.First(), replace) ?? contact.Name : contact.Name;

            return new EmailContact(name, contact.Address);
        }

        public IEnumerable<EmailContact> ParseEmailContacts(string value)
        {
            IEnumerable<EmailContact> contacts = null;
            if (!string.IsNullOrEmpty(value))
            {
                var emailSeparator = new char[] { ';', ',', ' ', '&', '|' };
                var emailAddresses = value.Split(emailSeparator, StringSplitOptions.RemoveEmptyEntries);
                contacts = emailAddresses.Select(email => GetContactFromEmailAddress(email));
            }
            return contacts ?? Enumerable.Empty<EmailContact>();
        }

        public EmailContact GetContactFromEmailAddress(string emailAddress)
        {
            var replace = new char[] { '_', '.', '-' };
            var email = emailAddress?.Split('@')?.FirstOrDefault();
            string name = ToSpaceReplaceTitleCase(email, replace) ?? string.Empty;
            return new EmailContact(name, emailAddress);
        }

        internal static string ToSpaceReplaceTitleCase(string value, char[] replace = null)
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

        public static string Replace(string value, char[] oldChars, char newChar)
        {
            string result = value?.Clone() as string ?? "";
            if (oldChars != null)
                foreach (var r in oldChars)
                    result = result.Replace(r, newChar);
            return result;
        }

        /// <summary> 
        /// Ensures that the last character on the extraction
        /// path is the directory separator "\\" char.
        /// </summary>
        /// <param name="filePath">Path to check</param>
        /// <returns>Modified path</returns>
        public string NormaliseFilePath(string filePath)
        {
            if (!Path.HasExtension(filePath) && !filePath.EndsWith(Path.DirectorySeparatorChar.ToString()))
                filePath = $"{filePath}{Path.DirectorySeparatorChar}";
            return filePath;
        }

        public bool FileCheckOk(string filePath, bool checkFile = false)
        {
            bool isExisting = false;

            if (!checkFile)
                filePath = NormaliseFilePath(filePath);

            var directory = Path.GetDirectoryName(filePath);

            if (!Directory.Exists(directory))
                _logger.LogWarning($"Folder not found: '{directory}'");
            else if (checkFile && !File.Exists(filePath))
                _logger.LogWarning($"File not found: '{filePath}'");
            else
                isExisting = true;
            return isExisting;
        }

        public async Task<Stream> GetFileStreamAsync(string filePath, CancellationToken ct = default)
        {
            const int BufferSize = 8192;
            var outputStream = new MemoryStream();
            if (FileCheckOk(filePath, true))
            {
                using (var source = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, useAsync: true))
                {
                    await source.CopyToAsync(outputStream, BufferSize, ct);
                };
                outputStream.Position = 0;
                _logger.LogDebug($"Loaded to file-stream: '{filePath}'");
            }
            return outputStream;
        }

        public MimeEntity GetMimePart(Stream stream, string fileName, string contentType = "", string contentId = "")
        {
            MimeEntity result = null;
            if (stream != null && stream.Length > 0)
            {
                stream.Position = 0; // reset stream position ready to read
                if (string.IsNullOrWhiteSpace(contentType))
                    contentType = MediaTypeNames.Application.Octet;
                if (string.IsNullOrWhiteSpace(contentId))
                    contentId = Path.GetFileNameWithoutExtension(Path.GetRandomFileName());
                //streamIn.CopyTo(streamOut, 8192);
                var attachment = MimeKit.ContentDisposition.Attachment;
                result = new MimePart(contentType)
                {
                    Content = new MimeContent(stream),
                    ContentTransferEncoding = ContentEncoding.Base64,
                    ContentDisposition = new MimeKit.ContentDisposition(attachment),
                    ContentId = contentId,
                    FileName = fileName
                };
            }
            return result;
        }

        public async Task<MimeEntity> GetMimeEntityFromFilePathAsync(string filePath, string mediaType = MediaTypeNames.Application.Octet)
        {
            MimeEntity result = null;
            if (!string.IsNullOrWhiteSpace(filePath))
            {
                var stream = await GetFileStreamAsync(filePath);
                if (stream != null)
                {
                    string fileName = Path.GetFileName(filePath);
                    string contentType = Path.GetExtension(fileName)
                        .Equals(".pdf", StringComparison.OrdinalIgnoreCase) ?
                            MediaTypeNames.Application.Pdf : mediaType;
                    result = GetMimePart(stream, fileName, contentType);
                }
            }
            return result;
        }

        public async Task<IEnumerable<MimeEntity>> GetMimeEntitiesFromFilePathsAsync(params string[] filePaths)
        {
            IEnumerable<MimeEntity> results = Array.Empty<MimeEntity>();
            if (filePaths != null && filePaths.Length == 1 && !string.IsNullOrWhiteSpace(filePaths[0]))
            {
                var separator = new char[] { '|' }; //';' is a valid attachment file name character
                filePaths = filePaths[0].Split(separator, StringSplitOptions.RemoveEmptyEntries);
            }
            if (filePaths?.Length > 1)
            {
                var mimeEntityTasks = filePaths.Select(name => GetMimeEntityFromFilePathAsync(name));
                var mimeEntities = await Task.WhenAll(mimeEntityTasks);
                results = mimeEntities.Where(entity => entity != null);
            }
            return results;
        }
    }
}
