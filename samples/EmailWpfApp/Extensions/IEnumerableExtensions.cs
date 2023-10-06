using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace EmailWpfApp.Extensions
{
    public static class IEnumerableExtensions
    {
        public static bool IsNullOrEmpty<T>([NotNullWhen(false)] 
            this IEnumerable<T> enumerable)
            => !enumerable?.Any() ?? true;

        public static bool IsNotNullOrEmpty<T>([NotNullWhen(true)] 
            this IEnumerable<T> enumerable)
            => enumerable != null && enumerable.Any();

        public static bool IsNullOrEmpty<T>([NotNullWhen(false)] 
            this ICollection<T> list) => !list.IsNotNullOrEmpty();

        public static bool IsNotNullOrEmpty<T>([NotNullWhen(true)] 
            this ICollection<T> list) => list?.Count > 0;

        public static string FirstOrBlank(
            this IEnumerable<string> enumerable)
            => enumerable?.FirstOrDefault() ?? "";

        public static string LastOrBlank(
            this IEnumerable<string> enumerable)
            => enumerable?.LastOrDefault() ?? "";

        public static ICollection<T> TryAdd<T>(
            this ICollection<T> list, T item) where T : class
        {
            if (list is null)
                list = new List<T>();
            if (item != null && !list.IsReadOnly)
                list.Add(item);
            return list ?? Array.Empty<T>();
        }

        public static ICollection<T> TryAddUnique<T>(
            this ICollection<T> list, T item)
        {
            if (list is null)
                list = new List<T>();
            if (item != null && !list.IsReadOnly && !list.Contains(item))
                list.Add(item);
            return list ?? Array.Empty<T>();
        }

        public static IEnumerable<T> Concatenate<T>(//[NotNull]
            this IEnumerable<T> seriesA, IEnumerable<T> seriesB)
        {
            if (seriesA is null)
                seriesA = seriesB ?? Enumerable.Empty<T>();
            else if (seriesB != null)
                seriesA = Enumerable.Concat(seriesA, seriesB);
            return seriesA;
        }

        public static async Task<IList<TResult>> RunAllAsync<TSource, TResult>(
            this IEnumerable<TSource> source, Func<TSource, Task<TResult>> method, ILogger _logger, CancellationToken ct = default)
        {
            IList<TResult>? results = null;
            try
            {
                var parallelTasks = new List<Task>();
                var tasks = source.Select(s => method(s));
                for (int i = 0; i < tasks.Count(); i++)
                {
                    var task = tasks.ElementAt(i).ContinueWith(t =>
                        _logger.LogError(t.Exception?.InnerException ?? t.Exception,
                            "RunAllAsync #{0} ID{1} failed", i, Task.CurrentId), TaskContinuationOptions.OnlyOnFaulted);
                    parallelTasks.Add(task);
                };
                results = await Task.WhenAll(tasks);
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("RunAllAsync Task ID{0} cancelled", Task.CurrentId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RunAllAsync Task ID{0} failed", Task.CurrentId);
            }
            return results ?? Array.Empty<TResult>();
        }

        public static void AddRange<T>([NotNull]
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

        public static IList<T> TryAddUniqueRange<T>(
            this IList<T> list, IEnumerable<T> items)
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

        public static List<T> TryAddRange<T>(
            this List<T> list, IEnumerable<T> items)
        {
            if (list != null && items != null)
                list.AddRange(items.Where(i => i != null));
            return list ?? new List<T>();
        }

        public static void TryRemove<T>(
            this IList<T> list, T itemToRemove)
        {
            if (list != null && itemToRemove != null)
                list.Remove(itemToRemove);
        }

        public static void RemoveRange<T>([NotNull]
            this IList<T> list, IEnumerable<T> itemsToRemove)
        {
            if (list is null)
                list = new List<T>();
            if (itemsToRemove != null)
            {
                if (list is List<T> listT)
                    listT.RemoveRange(itemsToRemove);
                else
                    foreach (T item in itemsToRemove)
                        list.Remove(item);
            }
        }

        public static IEnumerable<string> SplitToString(
            this string str, params char[] chars)
        {
            if (chars is null) chars = new char[] { ' ', ',', '.', '?', '!', '#', '/', '\\', '\r', '\n', '\t', '\'', '\"' };
            return str?.Split(chars, StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();
        }

        public static string ToEnumeratedString<T>(
            this IEnumerable<T> data, string div = ", ")
            => data is null ? "" : string.Join(div,
                data.Select(o => o?.ToString() ?? ""));

        public static int TrueCount(this IEnumerable<bool?> array)
            => array?.Count(t => t.GetValueOrDefault()) ?? 0;

        public static T Median<T>(IEnumerable<T> values)
        {
            if (values is null)
                throw new ArgumentNullException(nameof(values), "Values must not be null.");
            T result;
            if (values is IList<T> list)
                result = list[list.Count / 2];
            else
            {
                int halfLength = values.Count() / 2 - 1;
                if (halfLength < 0) halfLength = 0;
                result = values.Skip(halfLength).First();
            }
            return result;
        }

        public static IEnumerable<double> StandardDeviationResults<T>(
            this IEnumerable<T> values, byte numberOfStdDevs = 1, bool isStatisticalPopulation = true)
            where T : unmanaged, IComparable, IEquatable<T>, IConvertible
        {
            return values.StandardDeviationResults((v) => Convert.ToDouble(v), numberOfStdDevs, isStatisticalPopulation);
        }

        public static IEnumerable<double> StandardDeviationResults<T>(
            this IEnumerable<T> values, Func<T, double> decimals, byte numberOfStdDevs = 1, bool isStatisticalPopulation = true)
            where T : unmanaged, IComparable, IEquatable<T>, IConvertible
        {
            var sequence = values?.Select(decimals).ToList();
            var sd = sequence?.StandardDeviationResults(numberOfStdDevs, isStatisticalPopulation);
            return sd ?? Array.Empty<double>();
        }

        public static IEnumerable<double> StandardDeviationResults(
            this IEnumerable<double> values, byte numberOfStdDevs = 1, bool isStatisticalPopulation = true)
        {
            (double stdDev, double avg) = values.StandardDeviationAverage(isStatisticalPopulation);
            if (numberOfStdDevs > 1) stdDev *= numberOfStdDevs;
            (double min, double max) = (avg - stdDev, avg + stdDev);
            return values.Where(s => s >= min && s <= max);
        }

        public static (double, double) StandardDeviationAverage<T>(
            this IEnumerable<T> values, bool isStatisticalPopulation = true)
            where T : unmanaged, IComparable, IEquatable<T>, IConvertible
        {
            return values.StandardDeviationAverage((v) => Convert.ToDouble(v), isStatisticalPopulation);
        }

        public static (double, double) StandardDeviationAverage<T>(
            this IEnumerable<T> values, Func<T, double> decimals, bool isStatisticalPopulation = true)
        {
            if (values is null)
                throw new ArgumentNullException(nameof(values), "Values must not be null.");
            else if (decimals is null)
                throw new ArgumentNullException(nameof(decimals), "Function must not be null.");
            return values.Select(decimals).StandardDeviationAverage(isStatisticalPopulation);
        }

        public static (double, double) StandardDeviationAverage(
            this IEnumerable<double> values, bool isStatisticalPopulation = true)
        {
            int n = 0;
            var mean = 0.0;
            var sum = 0.0;
            var stdDev = 0.0;

            if (values != null)
            {
                foreach (var value in values)
                {
                    n++;
                    var delta = value - mean;
                    mean += delta / n;
                    sum += delta * (value - mean);
                }

                if (n > 1)
                {
                    var variance = isStatisticalPopulation ?
                        sum / n : sum / (n - 1);
                    stdDev = Math.Sqrt(variance);
                }
                else if (n == 1)
                    stdDev = Math.Sqrt(sum);
            }

            return (stdDev, mean);

        }

        public static void ActionEach<T>(
            this IEnumerable<T> items, Action<T> action, CancellationToken ct = default)
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

        public static Task ActionEachAsync<T>(this IEnumerable<T> sequence, Func<T, Task> action, CancellationToken ct = default)
        {
            return ct == default ? Task.WhenAll(sequence.Select(action)) :
                Task.WhenAll(sequence.Select(action).Select(task => Task.Run(() => task, ct)));
        }
    }
}
