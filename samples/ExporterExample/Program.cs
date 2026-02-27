using ExampleNamespace;
using ExporterExample.Abstractions;
using ExporterExample.Extensions;
using MailKitSimplified.Receiver;
using MailKitSimplified.Sender;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ExporterExample
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            var loggerConfiguration = new ConfigurationBuilder()
                .GetLoggerConfiguration();
            await Host.CreateDefaultBuilder(args)
                .BuildAndRunHostAsync(loggerConfiguration, builder =>
                    builder.ConfigureHost(loggerConfiguration));
        }

        private static IConfigurationRoot GetLoggerConfiguration(this IConfigurationBuilder builder, string fileName = "appsettings.serilog.json")
        {
            return builder.GetConfiguration(builder => builder.AddJsonFile(fileName));
        }

        private static IHostBuilder ConfigureHost(this IHostBuilder builder, IConfiguration configurationRoot)
        {
            var switchMappings = new Dictionary<string, string>()
            {
                { "--MailFolderName", "MailFolderName" },
                { "-m", "MailFolderName" },
                { "--ExportFolderPath", "ExportFolderPath" },
                { "-e", "ExportFolderPath" },
            };
            builder.InvokeConfiguration(configurationRoot, builder =>
                    builder.AddCommandLine(cli => cli.SwitchMappings = switchMappings))
                .ConfigureSerilog(configurationRoot)
                .ConfigureServices(Invoke);
            return builder;
        }

        private static void Invoke(HostBuilderContext context, IServiceCollection services)
        {
            services.AddTransient<IEmailService, EmailService>();
            services.AddHostedService<EmailBackgroundService>();
            services.AddMailKitSimplifiedEmailSender(context.Configuration);
            services.AddMailKitSimplifiedEmailReceiver(context.Configuration);
            services.ConfigureOpenTelemetry(context.Configuration, context.HostingEnvironment);
        }

        private static IHostBuilder InvokeConfiguration(this IHostBuilder builder, IConfiguration configurationRoot, Action<IConfigurationBuilder>? configure = null)
        {
            return builder.ConfigureAppConfiguration((context, configuration) =>
            {
                configuration.AddEnvironmentVariables(prefix: "OTEL_");
                configuration.AddConfiguration(configurationRoot);
                configure?.Invoke(configuration);
            });
        }
    }
}