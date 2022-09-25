using System;
using System.Linq;
using System.Threading;
using System.Collections.Generic;

namespace MailKitSimplified.Sender.Extensions
{
    public static class IEnumerableExtensions
    {
        public static string ToEnumeratedString<T>(
            this IEnumerable<T> data, string div = ", ")
            => data is null ? "" : string.Join(div,
                data.Select(o => o?.ToString() ?? ""));

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
