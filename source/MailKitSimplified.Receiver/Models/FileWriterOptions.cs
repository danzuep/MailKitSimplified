using System.Text.Json;

namespace MailKitSimplified.Receiver.Models
{
    public class FileWriterOptions
    {
        public const string SectionName = "FileWriter";

        /// <summary>
        /// File to write to.
        /// </summary>
        public string FilePath { get; set; } = string.Empty;

        /// <summary>
        /// Whether to append to an existing file (if there is one) or create a new one.
        /// </summary>
        public bool AppendToExisting { get; set; } = false;

        /// <summary>
        /// The length of time the file-writing text queue will idle for when empty.
        /// </summary>
        public ushort FileWriteMaxDelayMs { get; set; } = 100;

        public FileWriterOptions Copy() => MemberwiseClone() as FileWriterOptions;

        public override string ToString() => JsonSerializer.Serialize(this);
    }
}
