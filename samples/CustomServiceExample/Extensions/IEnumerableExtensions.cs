using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;

namespace CustomServiceExample.Extensions
{
    /// <summary>
    /// <seealso cref="https://github.com/dotnet/roslyn/blob/9806069c7c668ae938f783c77c2d3c453febca53/src/Compilers/Core/Portable/InternalUtilities/EnumerableExtensions.cs"/>
    /// </summary>
    public static class IEnumerableExtensions
    {
        public static bool IsNullOrEmpty<T>(//[NotNullWhen(false)]
            this IEnumerable<T> enumerable) => !enumerable?.Any() ?? true;

        public static bool IsNotNullOrEmpty<T>(//[NotNullWhen(true)]
            this IEnumerable<T> enumerable) => enumerable != null && enumerable.Any();

        public static bool IsNullOrEmpty<T>(//[NotNullWhen(false)]
            this ICollection<T> list) => !list.IsNotNullOrEmpty();

        public static bool IsNotNullOrEmpty<T>(//[NotNullWhen(true)]
            this ICollection<T> list) => list?.Count > 0;

        public static string FirstOrBlank(
            this IEnumerable<string> enumerable)
            => enumerable?.FirstOrDefault() ?? "";

        public static string LastOrBlank(
            this IEnumerable<string> enumerable)
            => enumerable?.LastOrDefault() ?? "";

        public static string ToEnumeratedString<T>(
            this IEnumerable<T> data, string div = ", ")
            => data is null ? "" : string.Join(div,
                data.Select(o => o?.ToString() ?? ""));

        public static IEnumerable<string> SplitToString(
            this string str, params char[] chars)
        {
            if (chars is null) chars = new char[] { ' ', ',', '.', '?', '!', '#', '/', '\\', '\r', '\n', '\t', '\'', '\"' };
            return str?.Split(chars, StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();
        }

        public static void AddRange<T>(//[NotNull]
            this IList<T> list, IEnumerable<T> items)
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

        public static IList<T> TryAdd<T>(
            this IList<T> list, T item)
        {
            if (list != null && item != null)
                list.Add(item);
            return list ?? Array.Empty<T>();
        }

        public static IList<TResult> FunctionAdd<TSource, TResult>(
            this IEnumerable<TSource> items, Func<TSource, TResult> method, CancellationToken ct = default)
        {
            IList<TResult> results = new List<TResult>();
            if (items != null)
                foreach (var item in items)
                    if (!ct.IsCancellationRequested)
                        results.Add(method(item));
            return results;
        }

        public static void ActionEach<T>(this IEnumerable<T> items,
            Action<T> action, CancellationToken ct = default)
        {
            if (items != null && action != null)
            {
                foreach (T item in items)
                {
                    if (ct.IsCancellationRequested)
                        break;
                    else if (item != null)
                        action(item);
                }
            }
        }
    }
}
