//using System.IO.Abstractions.TestingHelpers;
//using Microsoft.Extensions.Logging;
//using MailKitSimplified.Receiver.Services;

//namespace MailKitSimplified.Receiver.Tests
//{
//    public class MailKitProtocolLoggerUnitTests
//    {
//        private static readonly string _logFilePath = @"C:\Temp\Logs\ImapClient.txt";
//        private readonly MailKitProtocolLogger _mailKitProtocolLogger;

//        public MailKitProtocolLoggerUnitTests()
//        {
//            // Arrange
//            var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
//                { { _logFilePath, new MockFileData(string.Empty) } });
//            var loggerFactory = LoggerFactory.Create(_ => _.SetMinimumLevel(LogLevel.Debug).AddDebug());
//            _mailKitProtocolLogger = new MailKitProtocolLogger(loggerFactory.CreateLogger<MailKitProtocolLogger>());
//            _mailKitProtocolLogger.SetLogFilePath(_logFilePath, fileSystem: fileSystem);
//        }

//        [Fact]
//        public void LogTest()
//        {
//            _mailKitProtocolLogger.LogConnect(new Uri("http://localhost"));
//            _mailKitProtocolLogger.LogClient(new byte[] { }, 0, 0);
//            _mailKitProtocolLogger.LogServer(new byte[] { }, 0, 0);
//        }
//    }
//}