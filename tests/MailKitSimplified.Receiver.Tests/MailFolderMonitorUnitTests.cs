using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MailKitSimplified.Receiver.Abstractions;
using MailKitSimplified.Receiver.Services;
using MailKitSimplified.Receiver.Models;

namespace MailKitSimplified.Receiver.Tests
{
    public class MailFolderMonitorUnitTests
    {
        private static readonly Task _completedTask = Task.CompletedTask;
        private readonly Mock<IImapClient> _imapClientMock = new();
        private readonly Mock<IMailFolder> _mailFolderMock = new();
        private readonly Mock<IMailFolderClient> _mailFolderClientMock = new();
        private readonly Mock<IImapReceiver> _imapReceiverMock = new();
        private readonly MailFolderMonitor _imapIdleClient;

        public MailFolderMonitorUnitTests()
        {
            // Arrange
            var loggerFactory = LoggerFactory.Create(_ => _.SetMinimumLevel(LogLevel.Trace).AddDebug());
            _imapClientMock.SetupGet(_ => _.Capabilities).Returns(ImapCapabilities.Idle).Verifiable();
            _mailFolderClientMock.Setup(_ => _.ConnectAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(_mailFolderMock.Object).Verifiable();
            _imapReceiverMock.Setup(_ => _.ConnectMailFolderAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(_mailFolderMock.Object).Verifiable();
            _imapReceiverMock.Setup(_ => _.ConnectAuthenticatedImapClientAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(_imapClientMock.Object).Verifiable();
            _imapReceiverMock.SetupGet(_ => _.MailFolderClient)
                .Returns(_mailFolderClientMock.Object).Verifiable();
            var options = Options.Create(new FolderMonitorOptions { MessageFilter = MessageSummaryItems.Envelope });
            _imapIdleClient = new MailFolderMonitor(_imapReceiverMock.Object, options, loggerFactory.CreateLogger<MailFolderMonitor>());
        }

        [Fact]
        public void ToString_Verify()
        {
            // Act
            var description = _imapIdleClient.ToString();
            // Assert
            Assert.False(string.IsNullOrWhiteSpace(description));
            Assert.DoesNotContain(nameof(MailFolderMonitor), description);
        }

        private void SetupImapIdleClient()
        {
            //_imapIdleClient.MessageArrivalMethod = messageSummary => _completedTask;
            //_imapIdleClient.MessageDepartureMethod = messageSummary => _completedTask;
            _imapIdleClient.MessageArrivalMethod = messageSummary => null;
            _imapIdleClient.MessageDepartureMethod = messageSummary => null;
        }

        //[Fact]
        //public async Task MonitoryAsync_FromImapReceiver_Verify()
        //{
        //    SetupImapIdleClient();
        //    // Act
        //    await _imapIdleClient.IdleAsync(It.IsAny<CancellationToken>());
        //    // Assert
        //    _imapReceiverMock.Verify(_ => _.ConnectMailFolderAsync(It.IsAny<CancellationToken>()), Times.Once);
        //}

        //[Fact]
        //public async Task MonitoryAsync_ThrowsException()
        //{
        //    await Assert.ThrowsAsync<NotImplementedException>(() => _imapIdleClient.IdleAsync(It.IsAny<CancellationToken>()));
        //}
    }
}