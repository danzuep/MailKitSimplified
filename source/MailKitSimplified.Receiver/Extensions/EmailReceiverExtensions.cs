using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging;
using MimeKit;
using MimeKit.IO;
using MailKit;
using MailKit.Search;
using MailKit.Net.Imap;

namespace MailKitSimplified.Receiver.Extensions
{
    [ExcludeFromCodeCoverage]
    public static class EmailReceiverExtensions
    {
        public static string ToEnumeratedString<T>(this IEnumerable<T> data, string div = ", ") =>
            data is null ? "" : string.Join(div, data.Select(o => o?.ToString() ?? ""));

        public static void AddRange<T>(this IList<T> list, IEnumerable<T> items)
        {
            if (list is null)
                list = new List<T>();
            if (items != null)
            {
                if (list is List<T> listT)
                    listT.AddRange(items);
                else
                    foreach (T item in items)
                        if (item != null)
                            list.Add(item);
            }
        }

        internal static IList<string> GetFolderList(this ImapClient client, ILogger logger = null)
        {
            IList<string> mailFolderNames = new List<string>();
            if (client != null && client.IsAuthenticated)
            {
                if (logger == null)
                    logger = NullLogger.Instance;
                if (client.PersonalNamespaces.Count > 0)
                {
                    lock (client.SyncRoot)
                    {
                        var rootFolder = client.GetFolder(client.PersonalNamespaces[0]);
                        var subfolders = rootFolder.GetSubfolders().Select(f => f.Name);
                        var inboxSubfolders = client.Inbox.GetSubfolders().Select(f => f.FullName);
                        mailFolderNames.AddRange(inboxSubfolders);
                        mailFolderNames.AddRange(subfolders);
                        logger.LogDebug("{0} Inbox folders: {1}", subfolders.Count(),
                            inboxSubfolders.ToEnumeratedString());
                        logger.LogDebug("{0} personal folders: {1}", subfolders.Count(),
                            subfolders.ToEnumeratedString());
                    }
                }
                if (client.SharedNamespaces.Count > 0)
                {
                    lock (client.SyncRoot)
                    {
                        var rootFolder = client.GetFolder(client.SharedNamespaces[0]);
                        var subfolders = rootFolder.GetSubfolders().Select(f => f.Name);
                        mailFolderNames.AddRange(subfolders);
                        logger.LogDebug("{0} shared folders: {1}", subfolders.Count(),
                            subfolders.ToEnumeratedString());
                    }
                }
                if (client.OtherNamespaces.Count > 0)
                {
                    lock (client.SyncRoot)
                    {
                        var rootFolder = client.GetFolder(client.OtherNamespaces[0]);
                        var subfolders = rootFolder.GetSubfolders().Select(f => f.Name);
                        mailFolderNames.AddRange(subfolders);
                        logger.LogDebug("{0} other folders: {1}", subfolders.Count(),
                            subfolders.ToEnumeratedString());
                    }
                }
            }
            return mailFolderNames;
        }

        public static async Task<string> GetMailBodyTextAsync(
            this IMessageSummary mail, CancellationToken ct = default)
        {
            var textEntity = await mail.GetMailBodyEntityAsync(ct);
            return textEntity.GetBodyFullText();
        }

        public static string GetBodyFullText(this MimeEntity body) =>
            body is TextPart tp ? tp.Text ?? string.Empty : string.Empty;

        public static async Task<MimeEntity> GetMailBodyEntityAsync(
            this IMessageSummary mail, CancellationToken ct = default)
        {
            MimeEntity result = null;
            if (!mail?.Folder?.IsOpen ?? false)
                await mail.Folder.OpenAsync(FolderAccess.ReadOnly, ct);
            if (mail?.HtmlBody != null)
            {
                // this will download *just* the text/html part
                result = await mail.Folder.GetBodyPartAsync(mail.UniqueId, mail.HtmlBody, ct);

            }
            else if (mail?.TextBody != null)
            {
                // this will download *just* the text/plain part
                result = await mail.Folder.GetBodyPartAsync(mail.UniqueId, mail.TextBody, ct);
            }
            return result;
        }

        public static async Task<MimeMessage> Copy(
            this MimeMessage message, CancellationToken ct = default) =>
            await message.CloneStreamReferences(true, ct);

        public static async Task<MimeMessage> Clone(
            this MimeMessage message, CancellationToken ct = default) =>
            await message.CloneStreamReferences(false, ct);

        private static async Task<MimeMessage> CloneStreamReferences(
            this MimeMessage message, bool persistent, CancellationToken ct = default)
        {
            using (var memory = new MemoryBlockStream())
            {
                message.WriteTo(memory);
                memory.Position = 0;
                return await MimeMessage.LoadAsync(memory, persistent, ct);
            }
        }

        internal static SearchQuery BuildSubjectSearchQuery(string keywords)
        {
            // Split string into a list of queries and enumerate
            return keywords?.Split('|')
                ?.Select(key => SearchQuery.SubjectContains(key))
                ?.ToList()?.EnumerateOr() ??
                // return 'false' if null
                SearchQuery.Recent.And(SearchQuery.Old);
        }

        internal static SearchQuery BuildBodySearchQuery(string keywords)
        {
            // Split string into a list of queries and enumerate
            return keywords?.Split('|')
                ?.Select(key => SearchQuery.BodyContains(key))
                ?.ToList()?.EnumerateOr() ??
                // return 'false' if null
                SearchQuery.Recent.And(SearchQuery.Old);
        }

        internal static SearchQuery BuildDateSearchQuery(DateTime deliveredAfter, DateTime? deliveredBefore = null)
        {
            DateTime before = deliveredBefore != null ? deliveredBefore.Value : DateTime.Now;
            return SearchQuery.DeliveredAfter(deliveredAfter).And(SearchQuery.DeliveredBefore(before));
        }

        private static SearchQuery EnumerateOr<T>(
            this IList<T> queries) where T : SearchQuery
        {
            T query = queries.FirstOrDefault();

            if (queries?.Count > 1)
            {
                queries.Remove(query);
                // recursively return an 'Or' query
                return query.Or(EnumerateOr(queries));
            }

            return query;
        }

        public static async Task<IList<MimeMessage>> GetMimeMessagesAsync(
            this IMailFolder mailFolder, int limit = 10, int offset = 0, CancellationToken ct = default)
        {
            IList<MimeMessage> mimeMessages = new List<MimeMessage>();
            if (mailFolder != null)
            {
                if (!mailFolder.IsOpen)
                    await mailFolder.OpenAsync(FolderAccess.ReadOnly, ct);

                int start = mailFolder.Count < limit + offset ? offset : 0;
                int count = mailFolder.Count > limit ? limit : mailFolder.Count;
                for (int i = start; i < count; i++)
                {
                    var message = await mailFolder.GetMessageAsync(i, ct);

                    if (message != null)
                        mimeMessages.Add(message);

                    if (mailFolder.Access == FolderAccess.ReadWrite)
                        await mailFolder.AddFlagsAsync(i, MessageFlags.Seen, true, ct);
                }
            }
            return mimeMessages;
        }
    }
}
