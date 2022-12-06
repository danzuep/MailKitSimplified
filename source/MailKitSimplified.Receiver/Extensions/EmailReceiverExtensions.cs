using MimeKit;
using MailKit;
using MailKit.Search;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace MailKitSimplified.Receiver.Extensions
{
    [ExcludeFromCodeCoverage]
    public static class EmailReceiverExtensions
    {
        public static string ToEnumeratedString<T>(this IEnumerable<T> data, string div = ", ") =>
            data is null ? "" : string.Join(div, data.Select(o => o?.ToString() ?? ""));

        public static IList<T> TryAddUniqueRange<T>(this IList<T> list, IEnumerable<T> items)
        {
            var result = new List<T>();
            if (list is null)
                list = new List<T>();
            if (items != null)
            {
                foreach (T item in items)
                {
                    if (item != null && !list.Contains(item))
                    {
                        list.Add(item);
                        result.Add(item);
                    }
                }
            }
            return result;
        }

        public static void ActionEach<T>(this IEnumerable<T> items, Action<T> action, CancellationToken cancellationToken = default)
        {
            if (items != null && action != null)
            {
                foreach (T item in items)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;
                    else if (item != null)
                        action(item);
                }
            }
        }

        /// <summary>
        /// Downloads the text/html part of the body if it exists, otherwise downloads the text/plain part.
        /// </summary>
        /// <param name="mail"><see cref="IMessageSummary"/> body to download.</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns><see cref="TextPart"/> of the message body <see cref="MimeEntity"/>.</returns>
        public static async Task<string> GetBodyTextAsync(this IMessageSummary mail, CancellationToken cancellationToken = default)
        {
            MimeEntity textEntity = null;
            bool peekFolder = !mail?.Folder?.IsOpen ?? false;
            if (peekFolder)
                await mail.Folder.OpenAsync(FolderAccess.ReadOnly, cancellationToken);
            if (mail?.HtmlBody != null)
                textEntity = await mail.Folder.GetBodyPartAsync(mail.UniqueId, mail.HtmlBody, cancellationToken);
            else if (mail?.TextBody != null)
                textEntity = await mail.Folder.GetBodyPartAsync(mail.UniqueId, mail.TextBody, cancellationToken);
            if (peekFolder)
                await mail.Folder.CloseAsync(false, cancellationToken);
            var textPart = textEntity as TextPart;
            return textPart?.Text ?? string.Empty;
        }

        /// <summary>Recursively combine and return an 'Or' query.</summary>
        /// <param name="queries">List of queries to combine.</param>
        /// <returns>Queries combined with an 'Or' statement.</returns>
        public static SearchQuery EnumerateOr<T>(this IList<T> queries) where T : SearchQuery
        {
            T query = queries.FirstOrDefault();
            if (queries?.Count > 1)
            {
                queries.Remove(query);
                return query.Or(EnumerateOr(queries));
            }
            return query;
        }

        public static SearchQuery EnumerateOr(this IEnumerable<string> keywords, Func<string, SearchQuery> selector)
        {
            if (keywords == null)
                throw new ArgumentNullException(nameof(keywords));
            if (selector == null)
                throw new ArgumentNullException(nameof(selector));
            var query = keywords.Select(selector).ToList().EnumerateOr();
            return query ?? SearchQuery.Seen.And(SearchQuery.NotSeen);
        }
    }
}
