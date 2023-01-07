using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Debug;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;

namespace ControlLibrary.Helpers
{
    public static class LogProvider
    {
        private static IDictionary<string, ILogger> _loggers = new Dictionary<string, ILogger>();

        private static ILoggerFactory? _loggerFactory;
        internal static ILoggerFactory LoggerFactory
        {
            get
            {
                if (_loggerFactory == null)
                {
                    var debugProvider = new DebugLoggerProvider();
                    var consoleOptions = new OptionsMonitor<ConsoleLoggerOptions>(new ConsoleLoggerOptions());
                    var consoleProvider = new ConsoleLoggerProvider(consoleOptions);
                    var loggerProviders = new ILoggerProvider[] { debugProvider, consoleProvider };
                    _loggerFactory = new LoggerFactory(loggerProviders);
                }
                return _loggerFactory;
            }
            set
            {
                if (_loggerFactory == null)
                {
                    _loggerFactory = value;
                    _loggers.Clear();
                }
            }
        }

        public static void SetLoggerFactory(this IServiceCollection serviceCollection)
        {
            serviceCollection.BuildServiceProvider().SetLoggerFactory();
        }

        public static void SetLoggerFactory(this IServiceProvider serviceProvider)
        {
            LoggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        }

        private static ILogger CreateLogger(string name) =>
            LoggerFactory?.CreateLogger(name) ?? NullLogger.Instance;

        public static ILogger GetLogger<T>() => GetLogger(typeof(T).Name);

        public static ILogger GetLogger(string category)
        {
            if (!_loggers.ContainsKey(category))
                _loggers.Add(category, CreateLogger(category));
            return _loggers[category];
        }

        public static void Dispose(string category)
        {
            if (_loggers.ContainsKey(category))
                _loggers.Remove(category);
        }

        public static void Dispose<T>() => Dispose(typeof(T).Name);
        public static void Dispose()
        {
            _loggers?.Clear();
            _loggerFactory?.Dispose();
        }
    }

    public class OptionsMonitor<T> : IOptionsMonitor<T>
    {
        private readonly T options;

        public OptionsMonitor(T options)
        {
            this.options = options;
        }

        public T CurrentValue => options;

        public T Get(string name) => options;

        public IDisposable OnChange(Action<T, string> listener) => new NullDisposable();

        private class NullDisposable : IDisposable
        {
            public void Dispose() { }
        }
    }
}
