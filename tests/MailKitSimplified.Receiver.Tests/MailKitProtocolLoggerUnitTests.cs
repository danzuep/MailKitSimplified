using System.Text;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using Microsoft.Extensions.Logging;
using MailKitSimplified.Receiver.Services;
using MailKitSimplified.Receiver.Models;

namespace MailKitSimplified.Receiver.Tests
{
    public class MailKitProtocolLoggerUnitTests
    {
        private static readonly Uri _localhost = new Uri("imap://localhost:143/?starttls=when-available");
        private static readonly byte[] _testBytes = Encoding.UTF8.GetBytes("* OK smtp4dev\r\nA00000000 CAPABILITY\r\n");
        private static readonly string _logFilePath = @"Logs\ImapClient.txt";
        private readonly ILoggerFactory _loggerFactory;
        private readonly IFileSystem _fileSystem;
        private readonly Mock<IProtocolLogger> _protocolLoggerMock = new();
        private readonly MailKitProtocolLogger _mailKitProtocolLogger;
        private readonly LogFileWriter _logFileWriter;

        public MailKitProtocolLoggerUnitTests()
        {
            // Arrange
            _fileSystem = new MockFileSystem(new Dictionary<string, MockFileData> { { _logFilePath, new MockFileData(string.Empty) } });
            _loggerFactory = LoggerFactory.Create(_ => _.SetMinimumLevel(LogLevel.Trace).AddDebug());
            _protocolLoggerMock.Setup(_ => _.LogConnect(It.IsAny<Uri>())).Verifiable();
            _protocolLoggerMock.Setup(_ => _.LogServer(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>())).Verifiable();
            _protocolLoggerMock.Setup(_ => _.LogClient(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>())).Verifiable();
            var stub = Mock.Of<IAuthenticationSecretDetector>(_ => _.DetectSecrets(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()) == new List<AuthenticationSecret>());
            _protocolLoggerMock.SetupGet(_ => _.AuthenticationSecretDetector).Returns(stub).Verifiable();
            _logFileWriter = new LogFileWriter(_loggerFactory.CreateLogger<LogFileWriter>(), _fileSystem);
            _mailKitProtocolLogger = new MailKitProtocolLogger(_loggerFactory.CreateLogger<MailKitProtocolLogger>(), _logFileWriter);
        }

        [Fact]
        public void SetLogFilePath_NoTimestamp()
        {
            var protocolLoggerOptions = new ProtocolLoggerOptions { TimestampFormat = null };
            var mailKitProtocolLogger = MailKitProtocolLogger.Create(protocolLoggerOptions, _loggerFactory.CreateLogger<MailKitProtocolLogger>(), _fileSystem);
            mailKitProtocolLogger.LogConnect(_localhost);
            mailKitProtocolLogger.LogServer(_testBytes, 0, 15);
            mailKitProtocolLogger.LogClient(_testBytes, 15, 21);
        }

        [Fact]
        [Obsolete]
        public void SetLogFilePath_ToConsole()
        {
            _mailKitProtocolLogger.SetLogFilePath("Console");
            _mailKitProtocolLogger.LogServer(_testBytes, 0, 15);
        }

        [Fact]
        [Obsolete]
        public void SetLogFilePath_ToLogFile()
        {
            _mailKitProtocolLogger.SetLogFilePath(_logFilePath);
            _mailKitProtocolLogger.LogServer(_testBytes, 0, 15);
        }
    }
}