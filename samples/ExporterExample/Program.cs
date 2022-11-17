using ExporterExample.Models;
using ExporterExample.Services;
using MailKitSimplified.Sender;
using MailKitSimplified.Receiver;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace ExporterExample
{
    public class Program
    {
        static async Task Main(string[] args)
        {
            IHost host = Host.CreateDefaultBuilder(args)
                .ConfigureServices((context, services) =>
                {
                    //services.AddTransient<ExporterExample.EmailService>();
                    services.AddMailKitSimplifiedEmailSender(context.Configuration);
                    services.AddMailKitSimplifiedEmailReceiver(context.Configuration);
                })
                .Build();

            await host.RunAsync();

            var config = GetConfiguration(args).Get<ConsoleOptions>();
            if (!string.IsNullOrEmpty(config?.MailFolderName) && !string.IsNullOrEmpty(config.ExportFolderPath))
            {
                await Exporter.Create(useDebugLogger: true).ExportToFileAsync(config.MailFolderName, config.ExportFolderPath);
            }
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
            var builder = new ConfigurationBuilder().AddCommandLine(args, switchMappings);
            return builder.Build();
        }
    }
}