using System.IO.Abstractions;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Globalization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MailKit;
using CsvHelper;
using CsvHelper.Configuration;
using MailKitSimplified.Receiver.Services;
using MailKitSimplified.Receiver.Abstractions;
using ExporterExample.Abstractions;
using ExporterExample.Extensions;

namespace ExporterExample.Services
{
    public interface IExporter
    {
        void ExportFile(IEnumerable<IMessageSummary> mimeMessageSummary, string mailFolderName, string folderPathExport, string csvFolderSuffix = "", string jsonFolderSuffix = "");
    }
        
    public sealed class Exporter : IExporter
    {
        private static readonly JsonSerializerOptions _jsonSerializerOptions = new()
        {
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter(null, allowIntegerValues: false) }
        };

        private readonly IMailFolderReader _mailFolderReader;
        private readonly IFileSystem _fileSystem;
        private readonly ILogger _logger;

        public Exporter(IMailFolderReader mailFolderReader, IFileSystem? fileSystem = null, ILogger<Exporter>? logger = null)
        {
            _mailFolderReader = mailFolderReader;
            _fileSystem = fileSystem ?? new FileSystem();
            _logger = logger ?? NullLogger<Exporter>.Instance;
        }

        public static Exporter Create(bool useDebugLogger = false)
        {
            if (useDebugLogger)
            {
                var loggerFactory = LoggerFactory.Create(_ => _.SetMinimumLevel(LogLevel.Debug).AddDebug().AddConsole());
                using var imapReceiver = ImapReceiver.Create("imap.example.com", 0, "U5ern@me", "P@55w0rd");
                using var folderReceiver = new MailFolderReader(imapReceiver, loggerFactory.CreateLogger<MailFolderReader>());
                var exporter = new Exporter(folderReceiver, new FileSystem(), loggerFactory.CreateLogger<Exporter>());
                return exporter;
            }
            else
            {
                var imapReceiver = ImapReceiver.Create("imap.example.com", 0, "U5ern@me", "P@55w0rd");
                var folderReceiver = new MailFolderReader(imapReceiver);
                var exporter = new Exporter(folderReceiver, new FileSystem());
                return exporter;
            }
        }

        /// <summary>
        /// Export emails to CSV or JSON.
        /// </summary>
        /// <param name="mimeMessageSummary">Email to export</param>
        /// <param name="mailFolderName">Export file path</param>
        /// <param name="folderPathExport">Export file path</param>
        /// <param name="csvFolderSuffix">CSV folder suffix, null to disable</param>
        /// <param name="jsonFolderSuffix">JSON folder suffix, null to disable</param>
        public void ExportFile(IEnumerable<IMessageSummary> mimeMessageSummary, string mailFolderName, string folderPathExport, string csvFolderSuffix = "-csv", string jsonFolderSuffix = "-json")
        {
            var emailDtos = mimeMessageSummary.Select(m => m.ToDto());
            ExportFile(emailDtos, mailFolderName, folderPathExport, csvFolderSuffix, jsonFolderSuffix);
        }

        /// <summary>
        /// Export email.
        /// </summary>
        /// <param name="emails">Emails to export</param>
        /// <param name="folderPathExport">Export file path</param>
        /// <param name="csvFolderSuffix">CSV folder suffix, null to disable</param>
        /// <param name="jsonFolderSuffix">JSON folder suffix, null to disable</param>
        private void ExportFile(IEnumerable<IEmailDto> emails, string mailFolderName, string folderPathExport, string csvFolderSuffix = "-csv", string jsonFolderSuffix = "-json")
        {
            var folderPathCsv = csvFolderSuffix == null ? null : string.IsNullOrWhiteSpace(csvFolderSuffix) ? folderPathExport : $"{folderPathExport}{csvFolderSuffix}";
            if (!string.IsNullOrWhiteSpace(folderPathCsv))
            {
                _ = ExportCsv(emails, folderPathCsv, mailFolderName);
            }
            var folderPathJson = jsonFolderSuffix == null ? null : string.IsNullOrWhiteSpace(jsonFolderSuffix) ? folderPathExport : $"{folderPathExport}{jsonFolderSuffix}";
            if (!string.IsNullOrWhiteSpace(folderPathJson))
            {
                _ = ExportJson(emails, folderPathJson, mailFolderName);
            }
        }

        /// <summary>
        /// Export data as JSON.
        /// </summary>
        /// <param name="emails">Emails to export</param>
        /// <param name="folderPath">Export directory</param>
        /// <param name="mailFolderName">File name without a suffix</param>
        /// <param name="fileSuffix">"json" or "txt"</param>
        /// <returns>JSON</returns>
        public string ExportJson(IEnumerable<IEmailDto> emails, string folderPath, string mailFolderName, string fileSuffix = "json")
        {
            _fileSystem.Directory.CreateDirectory(folderPath);
            string filePath = $"{_fileSystem.Path.Combine(folderPath, mailFolderName)}.{fileSuffix}";
            string jsonString = JsonSerializer.Serialize(emails, _jsonSerializerOptions);
            _fileSystem.File.WriteAllText(filePath, jsonString);
            _logger.LogDebug($"Log exported to '{filePath}'.");
            return filePath;
        }

        /// <summary>
        /// Export data as CSV.
        /// </summary>
        /// <param name="emails">Emails to export</param>
        /// <param name="folderPath">Export directory</param>
        /// <param name="fileName">File name without a suffix</param>
        /// <param name="fileSuffix">"csv" or "txt"</param>
        /// <returns>CSV</returns>
        public string ExportCsv(IEnumerable<IEmailDto> emails, string folderPath, string fileName, string fileSuffix = "csv")
        {
            _fileSystem.Directory.CreateDirectory(folderPath);
            string filePath = $"{_fileSystem.Path.Combine(folderPath, fileName)}.{fileSuffix}";
            using (var writer = new StreamWriter(_fileSystem.File.Open(filePath, FileMode.Create)))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                csv.Context.RegisterClassMap<ExportMap>();
                csv.WriteRecords(emails);
            }
            _logger.LogDebug($"Log exported to '{filePath}'.");
            return filePath;
        }

        private class ExportMap : ClassMap<IEmailDto>
        {
            public ExportMap()
            {
                Map(m => m.Date).TypeConverterOption.Format("s");
                Map(m => m.From);
                Map(m => m.To);
            }
        }
    }
}
