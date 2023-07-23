using MailKit;
using System;
using System.IO;
using System.IO.Abstractions;
using System.Text.Json;

namespace MailKitSimplified.Receiver.Models
{
    public sealed class ProtocolLoggerOptions
    {
        public const string SectionName = "ProtocolLogger";
        public const string DefaultTimestampFormat = "yyyy-MM-ddTHH:mm:ssZ";
        public const string DefaultSmtpLogFilePath = "Logs/SmtpClient.txt";
        public const string DefaultImapLogFilePath = "Logs/ImapClient.txt";

        public FileWriterOptions FileWriter { get; set; } = new FileWriterOptions();

        public string TimestampFormat { get; set; } = null;

        public string ServerPrefix { get; set; } = "S: ";

        public string ClientPrefix { get; set; } = "C: ";

        public IProtocolLogger CreateProtocolLogger(IFileSystem fileSystem = null)
        {
            IProtocolLogger protocolLogger = null;
            if (FileWriter.FilePath?.Equals("console", StringComparison.OrdinalIgnoreCase) ?? false)
            {
                protocolLogger = new ProtocolLogger(Console.OpenStandardError());
            }
            else if (!string.IsNullOrWhiteSpace(FileWriter.FilePath))
            {
                bool isMockFileSystem = fileSystem != null &&
                    fileSystem.GetType().Name == "MockFileSystem";
                if (fileSystem == null)
                    fileSystem = new FileSystem();
                var directoryName = fileSystem.Path.GetDirectoryName(FileWriter.FilePath);
                if (!string.IsNullOrWhiteSpace(directoryName))
                    fileSystem.Directory.CreateDirectory(directoryName);
                if (isMockFileSystem)
                    protocolLogger = new ProtocolLogger(Stream.Null);
                else
                    protocolLogger = new ProtocolLogger(FileWriter.FilePath, FileWriter.AppendToExisting);
            }
            return protocolLogger;
        }

        public ProtocolLoggerOptions Copy() => MemberwiseClone() as ProtocolLoggerOptions;

        public override string ToString() => JsonSerializer.Serialize(this);
    }
}
