using Polly;
using Polly.RateLimit;
using Polly.Extensions.Http;
using Polly.Contrib.WaitAndRetry;
using System.Diagnostics;

namespace WebApiExample.Helpers
{
    public static class HttpPolicies
    {
        private static readonly int _maxRequestsBeforeCutoff = 20;
        private static readonly TimeSpan _rateLimitTimeWindow = TimeSpan.FromSeconds(1);
        private static readonly TimeSpan _retryDelay = _rateLimitTimeWindow / _maxRequestsBeforeCutoff;

        /// <summary>
        /// Set a limit on the number of transactions per second.
        /// <seealso href="https://github.com/App-vNext/Polly/wiki/Rate-Limit#rate-limit-v723-onwards"/>
        /// <seealso href="https://github.com/App-vNext/Polly/blob/master/README.md#rate-limit"/>
        /// </summary>
        public static readonly IAsyncPolicy<HttpResponseMessage> RateLimit =
            Policy.RateLimitAsync(numberOfExecutions: _maxRequestsBeforeCutoff, _rateLimitTimeWindow, maxBurst: _maxRequestsBeforeCutoff)
                .AsAsyncPolicy<HttpResponseMessage>();

        /// <summary>
        /// Wait and retry policy
        /// <seealso href="https://github.com/App-vNext/Polly-Samples/blob/master/PollyDemos/Async/AsyncDemo09_Wrap-Fallback-Timeout-WaitAndRetry.cs"/>
        /// </summary>
        public static readonly IAsyncPolicy<HttpResponseMessage> RetryAfterWait =
            Policy.Handle<RateLimitRejectedException>()
                .WaitAndRetryAsync(retryCount: 1, sleepDurationProvider: (retryCount) =>
                {
                    Debug.WriteLine($"Attempting HTTP policy retry #{retryCount} in {_retryDelay.Milliseconds}ms");
                    return _retryDelay;
                })
                .AsAsyncPolicy<HttpResponseMessage>();

        public static readonly IAsyncPolicy<HttpResponseMessage> ExponentialRetry =
            HttpPolicyExtensions.HandleTransientHttpError()
                .WaitAndRetryAsync(Backoff.DecorrelatedJitterBackoffV2(medianFirstRetryDelay: TimeSpan.FromSeconds(1), retryCount: 5));

        public static readonly IAsyncPolicy<HttpResponseMessage> CircuitBreaker =
            HttpPolicyExtensions.HandleTransientHttpError()
                .CircuitBreakerAsync(handledEventsAllowedBeforeBreaking: 6, durationOfBreak: TimeSpan.FromMinutes(1));
    }
}