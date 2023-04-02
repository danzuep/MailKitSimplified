using System.Text;
using System.Threading.Tasks;
using System.IO.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MailKitSimplified.Receiver.Models;
using MailKitSimplified.Receiver.Abstractions;

namespace MailKitSimplified.Receiver.Services
{
    public class LogFileWriter : IFileWriter, ILogFileWriter
    {
        private readonly ILogger _logger;
        private readonly IFileSystem _fileSystem;
        private readonly FileWriterOptions _fileWriteOptions;

        public LogFileWriter(ILogger<LogFileWriter> logger, IOptions<FileWriterOptions> options = null, IFileSystem fileSystem = null) :
            this(options, logger, fileSystem)
        {
        }

        private LogFileWriter(IOptions<FileWriterOptions> options = null, ILogger logger = null, IFileSystem fileSystem = null)
        {
            _logger = logger ?? NullLogger<LogFileWriter>.Instance;
            _fileSystem = fileSystem ?? new FileSystem();
            _fileWriteOptions = options?.Value ?? new FileWriterOptions();
        }

        public static LogFileWriter Create(ILogger logger, IOptions<FileWriterOptions> options = null, IFileSystem fileSystem = null) =>
            new LogFileWriter(options, logger, fileSystem);

        public static LogFileWriter Create(FileWriterOptions fileWriterOptions, ILogger logger = null, IFileSystem fileSystem = null)
        {
            var options = Options.Create(fileWriterOptions);
            var logFileWriter = new LogFileWriter(options, logger, fileSystem);
            return logFileWriter;
        }

        public void Write(StringBuilder stringBuilder)
        {
            var textToWrite = LogFileWriterQueue.RemoveLastCharacter(stringBuilder);
            WriteLine(textToWrite);
        }

        public void WriteLine(string textToWrite)
        {
            WriteLineAsync(textToWrite).GetAwaiter().GetResult();
        }

        public async Task WriteLineAsync(string textToWrite)
        {
            if (!string.IsNullOrWhiteSpace(_fileWriteOptions.FilePath))
            {
                var directoryName = _fileSystem.Path.GetDirectoryName(_fileWriteOptions.FilePath);
                if (!string.IsNullOrWhiteSpace(directoryName))
                    _fileSystem.Directory.CreateDirectory(directoryName);
                _logger.LogDebug($"Writing buffered text to file: {_fileWriteOptions.FilePath}");
                using (var streamWriter = _fileWriteOptions.AppendToExisting ?
                    _fileSystem.File.AppendText(_fileWriteOptions.FilePath) :
                    _fileSystem.File.CreateText(_fileWriteOptions.FilePath))
                {
                    await streamWriter.WriteLineAsync(textToWrite).ConfigureAwait(false);
                }
            }
        }

        public Task<string> ReadAllTextAsync()
        {
            var textReadFromFile = !string.IsNullOrWhiteSpace(_fileWriteOptions.FilePath) ?
                _fileSystem.File.ReadAllText(_fileWriteOptions.FilePath) : string.Empty;
            return Task.FromResult(textReadFromFile);
        }

        public void Dispose()
        {
        }

        public override string ToString() => _fileWriteOptions.ToString();
    }
}
