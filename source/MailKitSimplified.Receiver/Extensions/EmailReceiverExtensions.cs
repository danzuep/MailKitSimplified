using MailKit.Search;
using System;
using System.Linq;
using System.Threading;
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

        /// <summary>Recursively combine and return an 'Or' query.</summary>
        /// <param name="queries">List of queries to combine.</param>
        /// <returns>Queries combined with an 'Or' statement.</returns>
        public static SearchQuery EnumerateOr(this IList<SearchQuery> queries)
        {
            SearchQuery query = queries.FirstOrDefault();
            if (queries?.Count > 1)
            {
                queries.Remove(query);
                return query.Or(EnumerateOr(queries));
            }
            return query;
        }

        public static SearchQuery MatchAny(this IEnumerable<string> keywords, Func<string, SearchQuery> selector)
        {
            if (keywords == null)
                throw new ArgumentNullException(nameof(keywords));
            if (selector == null)
                throw new ArgumentNullException(nameof(selector));
            var query = EnumerateOr(keywords.Select(selector).ToList());
            return query ?? SearchQuery.All;
        }
    }
}
