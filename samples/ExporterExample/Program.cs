using ExporterExample.Models;
using ExporterExample.Services;
using Microsoft.Extensions.Configuration;

namespace ExporterExample
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var config = GetConfiguration(args).Get<ConsoleOptions>();
            if (!string.IsNullOrEmpty(config.FilePath))
            {
                //Exporter.Create(useDebugLogger: true).ExportFile(config.FilePath, config.FolderPath, verbose: config.VerboseExport);
            }
            else if (!string.IsNullOrEmpty(config.FolderPath))
            {
                //Exporter.Create().ExportFolder($"{config.FolderPath}-bin", config.FolderPath, "*.s1L", verbose: config.VerboseExport);
            }
        }

        private static IConfigurationRoot GetConfiguration(string[] args)
        {
            var switchMappings = new Dictionary<string, string>()
            {
                { "--folder", "FolderPath" },
                { "--path", "FolderPath" },
                { "-p", "FolderPath" },
                { "--file", "FilePath" },
                { "-f", "FilePath" },
                { "--verbose", "VerboseExport" },
                { "-v", "VerboseExport" },
            };
            var builder = new ConfigurationBuilder().AddCommandLine(args, switchMappings);
            return builder.Build();
        }
    }
}