namespace ConsoleServiceExample
{
    public sealed class DefaultLogger : ILogger, ILoggerFactory
    {
        public static ILogger Instance { get; } = new DefaultLogger();
        public static ILoggerFactory Factory { get; } = new DefaultLogger();

        private readonly ILogger _logger;
        private readonly ILoggerFactory _loggerFactory;

        private DefaultLogger(ILoggerFactory? loggerFactory = null)
        {
            _loggerFactory = loggerFactory ?? LoggerFactory.Create(builder => builder
#if DEBUG
                //.AddSimpleConsole(o => { o.IncludeScopes = true; o.TimestampFormat = "HH:mm:ss.f "; })
                .SetMinimumLevel(LogLevel.Debug)
                .AddDebug()
#endif
                .AddConsole());

            _logger = _loggerFactory.CreateLogger(nameof(DefaultLogger));
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            return _logger.BeginScope(state);
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return _logger.IsEnabled(logLevel);
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            _logger.Log(logLevel, eventId, state, exception, formatter);
        }

        public void AddProvider(ILoggerProvider provider)
        {
            _loggerFactory.AddProvider(provider);
        }

        public ILogger CreateLogger(string categoryName)
        {
            return _loggerFactory.CreateLogger(categoryName);
        }

        public void Dispose()
        {
            _loggerFactory?.Dispose();
        }
    }
}