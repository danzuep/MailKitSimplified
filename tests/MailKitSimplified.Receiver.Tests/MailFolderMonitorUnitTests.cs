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
        private readonly CancellationTokenSource _arrival = new();
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
            _imapClientMock.Setup(_ => _.IdleAsync(_arrival.Token, It.IsAny<CancellationToken>()))
                .Returns(StubIdleAsync);
            _mailFolderClientMock.Setup(_ => _.ConnectAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(_mailFolderMock.Object).Verifiable();
            _imapReceiverMock.Setup(_ => _.ConnectMailFolderAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(_mailFolderMock.Object).Verifiable();
            _imapReceiverMock.Setup(_ => _.ConnectAuthenticatedImapClientAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(_imapClientMock.Object).Verifiable();
            _imapReceiverMock.SetupGet(_ => _.MailFolderClient)
                .Returns(_mailFolderClientMock.Object).Verifiable();
            var options = Options.Create(new FolderMonitorOptions { MessageSummaryParts = MessageSummaryItems.Envelope });
            _imapIdleClient = new MailFolderMonitor(_imapReceiverMock.Object, options, loggerFactory.CreateLogger<MailFolderMonitor>());
        }

        private Task StubIdleAsync()
        {
            _imapClientMock.SetupGet(_ => _.IsIdle).Returns(true);
            _arrival.Cancel();
            return _completedTask;
        }


        [Fact]
        public void Copy_VerifyReturnsShallowCopy()
        {
            // Act
            var shallowCopy = _imapIdleClient.Copy();
            // Assert
            Assert.NotNull(shallowCopy);
            Assert.IsAssignableFrom<IMailFolderMonitor>(shallowCopy);
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

        private static Task OnArrivalAsync(IMessageSummary _) => _completedTask;

        [Fact]
        public async Task IdleAsync_WithNullMethods_ReturnsCompletedTask()
        {
            // Arrange
            _imapIdleClient.SetMaxRetries(0)
                .OnMessageArrival((messageSummary) => null)
                .OnMessageDeparture((messageSummary) => null);
            // Act
            await _imapIdleClient.IdleAsync(_arrival.Token);
            // Assert
            _imapReceiverMock.Verify(_ => _.ConnectMailFolderAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        //[Fact]
        //public async Task MonitoryAsync_FromImapReceiver_Verify()
        //{
        //    var imapIdleClient = ((MailFolderMonitor)_imapIdleClient.Copy()).SetMessageSummaryParts()
        //        .SetProcessMailOnConnect().SetIdleMinutes().SetMaxRetries(1)
        //        .OnMessageArrival((messageSummary) => _completedTask)
        //        .OnMessageDeparture((messageSummary) => _completedTask);
        //    await imapIdleClient.IdleAsync(_arrival.Token);
        //}

        //[Fact]
        //public async Task MonitoryAsync_ThrowsException()
        //{
        //    await Assert.ThrowsAsync<NotImplementedException>(() => _imapIdleClient.IdleAsync(It.IsAny<CancellationToken>()));
        //}
    }
}