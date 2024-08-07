using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace MailKitSimplified.Receiver.Extensions
{
    /// <summary>
    /// <see href="https://learn.microsoft.com/en-us/dotnet/core/extensions/high-performance-logging"/>
    /// </summary>
    [ExcludeFromCodeCoverage]
    public static class LoggerExtensions
    {
        public static void Serialized<T>(this ILogger logger, T obj, LogLevel logLevel = LogLevel.Information) where T : class =>
            logger.Log(logLevel, "\"{Name}\": {JsonSerializedObject}", typeof(T).Name, obj.ToSerializedString());

        internal static Action<ILogger, Exception> LogAction<T>(string message, LogLevel logLevel, int id) =>
            LoggerMessage.Define(logLevel, new EventId(id, name: typeof(T).Name), message);

        public static void Log<T>(this ILogger logger, string message, LogLevel logLevel = LogLevel.Information, int id = 0) =>
            LogAction<T>(message, logLevel, id)(logger, null);

        public static void Log<T>(this ILogger logger, Exception ex, string message, LogLevel logLevel = LogLevel.Error, int id = 1) =>
            LogAction<T>(message, logLevel, id)(logger, ex);
    }
}