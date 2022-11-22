using System.Text;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using Microsoft.Extensions.Logging;
using MailKitSimplified.Receiver.Services;

namespace MailKitSimplified.Receiver.Tests
{
    public class MailKitProtocolLoggerUnitTests
    {
        private static readonly Uri _localhost = new Uri("imap://localhost:143/?starttls=when-available");
        private static readonly byte[] _testBytes = Encoding.UTF8.GetBytes("* OK smtp4dev\r\nA00000000 CAPABILITY\r\n");
        private static readonly string _logFilePath = @"Logs\ImapClient.txt";
        private readonly IFileSystem _fileSystem;
        private readonly Mock<IProtocolLogger> _protocolLoggerMock = new();
        private readonly MailKitProtocolLogger _mailKitProtocolLogger;

        public MailKitProtocolLoggerUnitTests()
        {
            // Arrange
            _fileSystem = new MockFileSystem(new Dictionary<string, MockFileData> { { _logFilePath, new MockFileData(string.Empty) } });
            var loggerFactory = LoggerFactory.Create(_ => _.SetMinimumLevel(LogLevel.Trace).AddDebug());
            _protocolLoggerMock.Setup(_ => _.LogConnect(It.IsAny<Uri>())).Verifiable();
            _protocolLoggerMock.Setup(_ => _.LogServer(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>())).Verifiable();
            _protocolLoggerMock.Setup(_ => _.LogClient(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>())).Verifiable();
            var stub = Mock.Of<IAuthenticationSecretDetector>(_ => _.DetectSecrets(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()) == new List<AuthenticationSecret>());
            _protocolLoggerMock.SetupGet(_ => _.AuthenticationSecretDetector).Returns(stub).Verifiable();
            _mailKitProtocolLogger = new MailKitProtocolLogger(loggerFactory.CreateLogger<MailKitProtocolLogger>(), _protocolLoggerMock.Object, _fileSystem);
        }

        [Fact]
        public void SetLogFilePath_UseTimestamp()
        {
            _mailKitProtocolLogger.SetLogFilePath(useTimestamp: true, redactSecrets: false);
            _mailKitProtocolLogger.LogConnect(_localhost);
            _mailKitProtocolLogger.LogServer(_testBytes, 0, 15);
            _mailKitProtocolLogger.LogClient(_testBytes, 15, 21);
        }

        [Fact]
        public void SetLogFilePath_ToConsole()
        {
            _mailKitProtocolLogger.SetLogFilePath(string.Empty);
            _mailKitProtocolLogger.LogServer(_testBytes, 0, 15);
        }

        [Fact]
        public void SetLogFilePath_ToLogFile()
        {
            _mailKitProtocolLogger.SetLogFilePath(_logFilePath);
            _mailKitProtocolLogger.LogServer(_testBytes, 0, 15);
        }
    }
}