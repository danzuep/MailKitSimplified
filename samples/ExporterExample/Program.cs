using ExporterExample.Models;
using ExporterExample.Services;
using Microsoft.Extensions.Configuration;

namespace ExporterExample
{
    public class Program
    {
        static async Task Main(string[] args)
        {
            var config = GetConfiguration(args).Get<ConsoleOptions>();
            if (!string.IsNullOrEmpty(config.MailFolderName) && !string.IsNullOrEmpty(config.ExportFolderPath))
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