using ExampleNamespace;
using ExporterExample.Abstractions;
using MailKitSimplified.Receiver;
using MailKitSimplified.Sender;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Sinks.OpenTelemetry;

namespace ExporterExample
{
    public class Program
    {
        static async Task Main(string[] args)
        {
            var configuration = GetConfiguration(args);

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .CreateLogger();
            Log.Information("Starting host...");

            try
            {
                using var host = BuildHost(args, configuration);
                await host.RunAsync();
                //var config = configuration.Get<ConsoleOptions>();
                //if (!string.IsNullOrEmpty(config?.MailFolderName) && !string.IsNullOrEmpty(config.ExportFolderPath))
                //{
                //    await Exporter.Create(useDebugLogger: true).ExportToFileAsync(config.MailFolderName, config.ExportFolderPath);
                //}
                Log.Information("Stopping host...");
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

        private static IHost BuildHost(string[] args, IConfigurationRoot configurationRoot)
        {
            return Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((context, configuration) =>
                {
                    configuration.AddEnvironmentVariables(prefix: "OTEL_");
                    configuration.AddConfiguration(configurationRoot);
                })
                .ConfigureServices((context, services) =>
                {
                    services.AddTransient<IEmailService, EmailService>();
                    services.AddHostedService<EmailBackgroundService>();
                    services.AddMailKitSimplifiedEmailSender(context.Configuration);
                    services.AddMailKitSimplifiedEmailReceiver(context.Configuration);
                    services.ConfigureOpenTelemetry(context.Configuration, context.HostingEnvironment);
                })
                .UseSerilog((context, configuration) =>
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
                })
                .Build();
        }

        private static IConfigurationRoot GetConfiguration(string[] args)
        {
            var switchMappings = new Dictionary<string, string>()
            {
                { "--MailFolderName", "MailFolderName" },
                { "-m", "MailFolderName" },
                { "--ExportFolderPath", "ExportFolderPath" },
                { "-e", "ExportFolderPath" },
            };
            var builder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.serilog.json")
                .AddCommandLine(args, switchMappings);
            return builder.Build();
        }
    }
}