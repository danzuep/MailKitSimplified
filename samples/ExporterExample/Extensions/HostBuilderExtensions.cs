using System.Diagnostics.CodeAnalysis;
using MailKitSimplified.Receiver;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Sinks.OpenTelemetry;

namespace ExporterExample.Extensions
{
    [ExcludeFromCodeCoverage]
    public static class HostBuilderExtensions
    {
        public static async Task BuildAndRunHostAsync(this IHostBuilder hostBuilder, IConfiguration loggerConfiguration, Action<IHostBuilder>? configureHost = null)
        {
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(loggerConfiguration)
                .CreateLogger();

            try
            {
                configureHost?.Invoke(hostBuilder);
                using var host = hostBuilder.Build();
                await host.RunAsync();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Host unexpectedly terminated.");
                throw;
            }
            finally
            {
                await Log.CloseAndFlushAsync();
            }
        }

        public static IHostBuilder ConfigureSerilog(this IHostBuilder builder, IConfiguration configurationRoot)
        {
            return builder.UseSerilog((context, configuration) =>
            {
                var serviceName = context.Configuration.GetValue(
                    OpenTelemetryExtensions.OtelServiceNameKey,
                    context.HostingEnvironment.ApplicationName);
                configuration.ReadFrom.Configuration(configurationRoot)
                    .Enrich.WithProperty("Application", serviceName)
                    .WriteTo.OpenTelemetry(options =>
                    {
                        var otlpEndpoint = context.Configuration.GetValue<string>(
                            OpenTelemetryExtensions.OtelEndpointKey);
                        if (!string.IsNullOrEmpty(otlpEndpoint))
                        {
                            options.Endpoint = otlpEndpoint;
                            options.Protocol = OtlpProtocol.Grpc;
                            options.ResourceAttributes = new Dictionary<string, object>
                            {
                                ["deployment.environment"] = context.HostingEnvironment.EnvironmentName,
                                ["service.name"] = context.HostingEnvironment.ApplicationName,
                                ["service.namespace"] = nameof(ExporterExample),
                                ["service.instance.id"] = Environment.MachineName
                            };
                        }
                    });
            });
        }

        public static IConfigurationRoot GetConfiguration(this IConfigurationBuilder builder, Action<IConfigurationBuilder> configure = null)
        {
            configure?.Invoke(builder);
            return builder.Build();
        }
    }
}
