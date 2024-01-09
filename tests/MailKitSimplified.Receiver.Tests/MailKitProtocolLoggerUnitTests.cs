using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using MailKitSimplified.Receiver.Services;
using MailKitSimplified.Receiver.Models;
using MailKitSimplified.Receiver.Abstractions;

namespace MailKitSimplified.Receiver.Tests
{
    public class MailKitProtocolLoggerUnitTests
    {
        private static readonly string _nl = Environment.NewLine;
        internal static readonly string LogFilePath = @"Logs\ImapClient.txt";
        internal static readonly string TestReply = $"* OK smtp4dev{_nl}A00000000 CAPABILITY{_nl}";
        internal readonly int TestReplyMid = 13 + _nl.Length;
        internal readonly int TestReplyEnd = 19 + _nl.Length;
        private static readonly Uri _localhost = new Uri("imap://localhost:143/?starttls=when-available");
        private static readonly byte[] _testBytes = Encoding.UTF8.GetBytes(TestReply);
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger<MailKitProtocolLogger> _logger;
        private readonly Mock<ILogFileWriter> _logFileWriterMock = new();
        private readonly Mock<IProtocolLogger> _protocolLoggerMock = new();
        private readonly MailKitProtocolLogger _mailKitProtocolLogger;

        public MailKitProtocolLoggerUnitTests()
        {
            // Arrange
            _loggerFactory = LoggerFactory.Create(_ => _.SetMinimumLevel(LogLevel.Trace).AddDebug());
            _logger = _loggerFactory.CreateLogger<MailKitProtocolLogger>();
            _protocolLoggerMock.Setup(_ => _.LogConnect(It.IsAny<Uri>())).Verifiable();
            _protocolLoggerMock.Setup(_ => _.LogServer(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>())).Verifiable();
            _protocolLoggerMock.Setup(_ => _.LogClient(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>())).Verifiable();
            var stub = Mock.Of<IAuthenticationSecretDetector>(_ => _.DetectSecrets(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()) == new List<AuthenticationSecret>());
            _protocolLoggerMock.SetupGet(_ => _.AuthenticationSecretDetector).Returns(stub).Verifiable();
            _logFileWriterMock.Setup(_ => _.WriteLine(It.IsAny<string>())).Verifiable();
            _logFileWriterMock.Setup(_ => _.ReadAllTextAsync()).ReturnsAsync(TestReply).Verifiable();
            _mailKitProtocolLogger = CreateMailKitProtocolLogger();
        }

        private MailKitProtocolLogger CreateMailKitProtocolLogger(ProtocolLoggerOptions? protocolLoggerOptions = null)
        {
            var options = Options.Create(protocolLoggerOptions ?? new ProtocolLoggerOptions());
            var mailKitProtocolLogger = new MailKitProtocolLogger(_logFileWriterMock.Object, options, _logger);
            return mailKitProtocolLogger;
        }

        [Fact]
        public void MailKitProtocolLogger_LogConnect()
        {
            _mailKitProtocolLogger.LogConnect(_localhost);
            _logFileWriterMock.Verify(_ => _.WriteLine(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public void MailKitProtocolLogger_LogServer()
        {
            _mailKitProtocolLogger.LogServer(_testBytes, 0, TestReplyMid);
            _logFileWriterMock.Verify(_ => _.WriteLine(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public void MailKitProtocolLogger_LogClient()
        {
            _mailKitProtocolLogger.LogClient(_testBytes, TestReplyMid, TestReplyEnd);
            _logFileWriterMock.Verify(_ => _.WriteLine(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public void SetLogFilePath_NoTimestamp()
        {
            var protocolLoggerOptions = new ProtocolLoggerOptions { TimestampFormat = null };
            var mailKitProtocolLogger = CreateMailKitProtocolLogger(protocolLoggerOptions);
            mailKitProtocolLogger.LogConnect(_localhost);
            mailKitProtocolLogger.LogServer(_testBytes, 0, TestReplyMid);
            mailKitProtocolLogger.LogClient(_testBytes, TestReplyMid, TestReplyEnd);
            //_logFileWriterMock.Verify(_ => _.WriteLine(It.IsAny<string>()), Times.Exactly(3));
        }
    }
}