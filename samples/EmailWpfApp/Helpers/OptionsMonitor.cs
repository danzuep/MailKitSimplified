using System;
using Microsoft.Extensions.Options;

namespace EmailWpfApp.Helpers
{
    public class OptionsMonitor<T> : IOptionsMonitor<T> where T : class
    {
        private readonly T _options;

        public OptionsMonitor(T options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public T CurrentValue => _options;

        public T Get(string? name) => _options;

        public IDisposable OnChange(Action<T, string> listener) => new NullDisposable();

        private class NullDisposable : IDisposable
        {
            public void Dispose() { }
        }
    }
}
