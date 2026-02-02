using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace MailKitSimplified.Receiver
{
    [ExcludeFromCodeCoverage]
    public static class OpenTelemetryExtensions
    {
        internal static readonly string OtelEndpointKey = "OTEL_EXPORTER_OTLP_ENDPOINT";
        internal static readonly string OtelServiceNameKey = "OTEL_SERVICE_NAME";
        internal static readonly string OtelHeadersKey = "OTEL_EXPORTER_OTLP_HEADERS";
        internal static readonly string TraceSamplingRatioKey = "OTEL_TRACES_SAMPLER_ARG";
        private static readonly string ServiceNamespace = nameof(MailKitSimplified);

        internal static void ConfigureOpenTelemetry(this IServiceCollection services, IConfiguration configuration, IHostEnvironment hostingEnvironment)
        {
            var otlpEndpoint = configuration.GetValue<string>(OtelEndpointKey);
            if (string.IsNullOrEmpty(otlpEndpoint))
            {
                return;
            }

            Sdk.SetDefaultTextMapPropagator(new CompositeTextMapPropagator(
            [
                new TraceContextPropagator(),
                new BaggagePropagator()
            ]));

            var otelCollector = new Uri(otlpEndpoint);
            var otelHeaders = configuration.GetValue<string>(OtelHeadersKey);
            var serviceName = configuration.GetValue(OtelServiceNameKey, hostingEnvironment.ApplicationName);
            var samplingRatio = configuration.GetValue(TraceSamplingRatioKey, 0.01);
            var traceSamplingRatio = Math.Clamp(samplingRatio, 0d, 1d);

            services.AddOpenTelemetry()
                .ConfigureResource(builder =>
                {
                    builder
                        .AddService(serviceName: serviceName,
                            serviceInstanceId: Environment.MachineName)
                        .AddTelemetrySdk()
                        .AddAttributes(
                        [
                            new("service.namespace", ServiceNamespace),
                            new("deployment.environment", hostingEnvironment.EnvironmentName)
                        ]);
                })
                .WithTracing(builder =>
                {
                    builder
                        .AddOtlpExporter(otlpOptions =>
                        {
                            otlpOptions.Endpoint = otelCollector;
                            if (string.IsNullOrWhiteSpace(otelHeaders))
                            {
                                otlpOptions.Headers = otelHeaders;
                            }
                        })
                        .AddSource(ServiceNamespace)
                        .AddAspNetCoreInstrumentation(options =>
                        {
                            options.RecordException = true;
                            options.EnrichWithHttpRequest = (activity, request) =>
                            {
                                if (request.ContentLength.HasValue)
                                {
                                    activity.SetTag("http.response_content_length", request.ContentLength.Value);
                                }
                            };
                            options.EnrichWithHttpResponse = (activity, response) =>
                            {
                                activity.SetTag("http.status_code", response.StatusCode);
                                if (response.ContentLength.HasValue)
                                {
                                    activity.SetTag("http.response_content_length", response.ContentLength.Value);
                                }
                            };
                        })
                        .AddHttpClientInstrumentation()
                        .SetSampler(new TraceIdRatioBasedSampler(traceSamplingRatio));
                })
                .WithMetrics(builder =>
                {
                    builder
                        .AddOtlpExporter(otlpOptions =>
                        {
                            otlpOptions.Endpoint = otelCollector;
                            if (string.IsNullOrWhiteSpace(otelHeaders))
                            {
                                otlpOptions.Headers = otelHeaders;
                            }
                        })
                        .AddMeter(ServiceNamespace)
                        .AddRuntimeInstrumentation()
                        .AddAspNetCoreInstrumentation()
                        .AddHttpClientInstrumentation();
                });
        }
    }
}