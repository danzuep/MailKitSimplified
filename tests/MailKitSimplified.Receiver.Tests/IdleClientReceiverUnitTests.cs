using Microsoft.Extensions.Logging;
using MailKitSimplified.Receiver.Abstractions;
using MailKitSimplified.Receiver.Services;

namespace MailKitSimplified.Receiver.Tests
{
    public class IdleClientReceiverUnitTests
    {
        private const string _localhost = "localhost";
        private readonly Mock<IImapClient> _imapClientMock = new();
        private readonly Mock<IMailFolder> _mailFolderMock = new();
        private readonly Mock<IMailFolderClient> _mailFolderClientMock = new();
        private readonly Mock<IImapReceiver> _imapReceiverMock = new();
        private readonly IdleClientReceiver _imapIdleClient;

        public IdleClientReceiverUnitTests()
        {
            // Arrange
            _imapClientMock.SetupGet(_ => _.Capabilities).Returns(ImapCapabilities.Idle).Verifiable();
            _mailFolderClientMock.Setup(_ => _.ConnectAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(_mailFolderMock.Object).Verifiable();
            _imapReceiverMock.Setup(_ => _.ConnectMailFolderAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(_mailFolderMock.Object).Verifiable();
            _imapReceiverMock.Setup(_ => _.ConnectImapClientAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(_imapClientMock.Object).Verifiable();
            _imapReceiverMock.Setup(_ => _.ConnectMailFolderClientAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(_mailFolderClientMock.Object).Verifiable();
            _imapIdleClient = new IdleClientReceiver(_imapReceiverMock.Object, Mock.Of<ILogger<IdleClientReceiver>>());
        }

        //[Fact]
        //public async Task MonitoryAsync_ThrowsException()
        //{
        //    await Assert.ThrowsAsync<NotImplementedException>(() => _imapIdleClient.MonitorAsync(MessageArrival));
        //}

        //[Fact]
        //public async Task MonitoryAsync_FromImapReceiver()
        //{
        //    await _imapIdleClient.MonitorAsync(MessageArrival);
        //}

        //[Fact]
        //public async Task ConnectImapClientAsync_VerifyType()
        //{
        //    // Act
        //    await _imapIdleClient.MonitorAsync(MessageArrival, It.IsAny<string>(), It.IsAny<CancellationToken>());
        //    // Assert
        //    _imapReceiverMock.Verify(_ => _.ConnectMailFolderAsync(It.IsAny<CancellationToken>()), Times.Once);
        //}

        private ValueTask MessageArrival(IList<IMessageSummary> messages) => throw new NotImplementedException();//ValueTask.CompletedTask;

        //[Fact]
        //public void ToString_Verify()
        //{
        //    // Act
        //    var description = _imapIdleClient.ToString();
        //    // Assert
        //    Assert.False(string.IsNullOrWhiteSpace(description));
        //    Assert.DoesNotContain(nameof(IdleClientReceiver), description);
        //}
    }
}