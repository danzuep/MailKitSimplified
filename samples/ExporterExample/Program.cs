using ExporterExample.Models;
using ExporterExample.Services;
using MailKitSimplified.Receiver;
using MailKitSimplified.Sender;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;

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

            try
            {
                using var host = BuildHost(args);
                await host.RunAsync();

                var config = configuration.Get<ConsoleOptions>();
                if (!string.IsNullOrEmpty(config?.MailFolderName) && !string.IsNullOrEmpty(config.ExportFolderPath))
                {
                    await Exporter.Create(useDebugLogger: true).ExportToFileAsync(config.MailFolderName, config.ExportFolderPath);
                }
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

        private static IHost BuildHost(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                //.ConfigureAppConfiguration((context, configuration) => { })
                .ConfigureServices((context, services) =>
                {
                    //services.AddTransient<ExporterExample.EmailService>();
                    services.AddMailKitSimplifiedEmailSender(context.Configuration);
                    services.AddMailKitSimplifiedEmailReceiver(context.Configuration);
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