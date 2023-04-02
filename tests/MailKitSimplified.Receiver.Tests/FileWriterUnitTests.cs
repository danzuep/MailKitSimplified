using System.Text;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using Microsoft.Extensions.Logging;
using MailKitSimplified.Receiver.Services;
using MailKitSimplified.Receiver.Models;
using Microsoft.Extensions.Options;

namespace MailKitSimplified.Receiver.Tests
{
    public class FileWriterUnitTests
    {
        private static readonly string _logFilePath = MailKitProtocolLoggerUnitTests.LogFilePath;
        private const string _testReply = MailKitProtocolLoggerUnitTests.TestReply;
        private readonly IFileSystem _fileSystem = new MockFileSystem();
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger<LogFileWriterQueue> _logger;
        private readonly LogFileWriterQueue _logFileWriter;

        public FileWriterUnitTests()
        {
            // Arrange
            _loggerFactory = LoggerFactory.Create(_ => _.SetMinimumLevel(LogLevel.Trace).AddDebug());
            _logger = _loggerFactory.CreateLogger<LogFileWriterQueue>();
            _logFileWriter = CreateThreadsafeLogFileWriter(new FileWriterOptions { FilePath = _logFilePath });
        }

        private LogFileWriterQueue CreateThreadsafeLogFileWriter(FileWriterOptions fileWriterOptions)
        {
            var options = Options.Create(fileWriterOptions);
            var fileWriter = new LogFileWriterQueue(options, _logger, _fileSystem);
            return fileWriter;
        }

        [Theory]
        [InlineData(_testReply)]
        public async Task LogFileWriter_WriteAsync(string textToWrite)
        {
            _logFileWriter.Write(new StringBuilder(textToWrite));
            var textFromFile = await _logFileWriter.ReadAllTextAsync();
            Assert.Equal(_testReply, textFromFile);
        }

        [Theory]
        [InlineData(_testReply)]
        public async Task LogFileWriter_WriteTwiceAsync(string textToWrite)
        {
            _logFileWriter.Write(new StringBuilder(textToWrite));
            _logFileWriter.Write(new StringBuilder(textToWrite));
            var textFromFile = await _logFileWriter.ReadAllTextAsync();
            Assert.Equal($"{_testReply}{_testReply}", textFromFile);
        }
    }
}