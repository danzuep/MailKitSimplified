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

        public string TimestampFormat { get; set; } = DefaultTimestampFormat;

        public string ServerPrefix { get; set; } = "S: ";

        public string ClientPrefix { get; set; } = "C: ";

        public ProtocolLoggerOptions Copy() => MemberwiseClone() as ProtocolLoggerOptions;

        public override string ToString() => JsonSerializer.Serialize(this);
    }
}
