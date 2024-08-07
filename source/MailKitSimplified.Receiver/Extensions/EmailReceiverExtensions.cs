using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json;
using System.Threading;
using MailKit;
using MailKit.Search;

namespace MailKitSimplified.Receiver.Extensions
{
    [ExcludeFromCodeCoverage]
    public static class EmailReceiverExtensions
    {
        /// <inheritdoc cref="string.Join"/>
        public static string ToEnumeratedString<T>(this IEnumerable<T> data, string delimiter = ", ") =>
            data is null ? string.Empty : string.Join(delimiter, data.Select(o => o?.ToString() ?? string.Empty));

        public static string ToSerializedString(this object obj) =>
            obj != null ? JsonSerializer.Serialize(obj) : string.Empty;

        /// <inheritdoc cref="List{T}.AddRange(IEnumerable{T})"/>
        public static IList<T> TryAddUniqueRange<T>(this IList<T> list, IEnumerable<T> items) where T : IMessageSummary        {
            var result = new List<T>();
            if (list is null)
                list = new List<T>();
            if (items != null)
            {
                foreach (T item in items.Where(t => t != null && !list.Any(m => m.UniqueId == t.UniqueId)))
                {
                    list.Add(item);
                    result.Add(item);
                }
            }
            return result;
        }

        /// <inheritdoc cref="List{T}.ForEach(Action{T})"/>
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

        private static SearchQuery Enumerate(this IEnumerable<SearchQuery> queries, bool or = true)
        {
            SearchQuery fullQuery = null;
            if (queries != null)
            {
                foreach (var query in queries)
                {
                    fullQuery = fullQuery == null ? query : or ?
                        fullQuery.Or(query) : fullQuery.And(query);
                }
            }
            return fullQuery ?? SearchQuery.All;
        }

        /// <summary>Combine and return an 'And' query.</summary>
        /// <param name="queries">List of queries to combine.</param>
        /// <returns>Queries combined with an 'And' statement.</returns>
        public static SearchQuery EnumerateAnd(this IEnumerable<SearchQuery> queries) =>
            Enumerate(queries, or: false);

        /// <summary>Combine and return an 'Or' query.</summary>
        /// <param name="queries">List of queries to combine.</param>
        /// <returns>Queries combined with an 'Or' statement.</returns>
        public static SearchQuery EnumerateOr(this IEnumerable<SearchQuery> queries) =>
            Enumerate(queries, or: true);

        /// <summary>Combine and return an 'Or' query of keywords.</summary>
        /// <param name="keywords">List of keywords to combine.</param>
        /// <returns>Queries combined with an 'Or' statement.</returns>
        public static SearchQuery MatchAny(this IEnumerable<string> keywords, Func<string, SearchQuery> selector)
        {
            if (keywords == null)
                throw new ArgumentNullException(nameof(keywords));
            if (selector == null)
                throw new ArgumentNullException(nameof(selector));
            var query = Enumerate(keywords.Select(selector), or: true);
            return query;
        }
    }
}
