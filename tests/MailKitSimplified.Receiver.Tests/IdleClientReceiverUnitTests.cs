using Microsoft.Extensions.Logging;
using MailKitSimplified.Receiver.Abstractions;

namespace MailKitSimplified.Receiver.Tests
{
    public class IdleClientReceiverUnitTests
    {
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
            _imapReceiverMock.Setup(_ => _.ConnectMailFolderAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(_mailFolderMock.Object).Verifiable();
            _imapReceiverMock.Setup(_ => _.ConnectImapClientAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(_imapClientMock.Object).Verifiable();
            _imapReceiverMock.Setup(_ => _.ConnectMailFolderClientAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(_mailFolderClientMock.Object).Verifiable();
            _imapIdleClient = new IdleClientReceiver(_imapReceiverMock.Object, Mock.Of<ILogger<IdleClientReceiver>>());
        }

        [Fact]
        public void CreateIdleClientReceiver_WithImapReceiver()
        {
            var imapIdleClient = new IdleClientReceiver(_imapReceiverMock.Object);
            Assert.NotNull(imapIdleClient);
        }

        [Fact]
        public void CreateIdleClientReceiver_WithHostName()
        {
            var imapIdleClient = IdleClientReceiver.Create("localhost");
            Assert.NotNull(imapIdleClient);
        }

        //[Fact]
        //public async Task MonitoryAsync_FromImapReceiver()
        //{
        //    using var imapReceiver = ImapReceiver.Create("localhost");
        //    await imapReceiver.Folder("INBOX").MonitorAsync();
        //    Assert.NotNull(imapReceiver);
        //}

        //[Fact]
        //public async Task ConnectImapClientAsync_VerifyType()
        //{
        //    // Act
        //    await _imapIdleClient.MonitorAsync(MessageArrival, It.IsAny<string>(), It.IsAny<CancellationToken>());
        //    // Assert
        //    _imapReceiverMock.Verify(_ => _.ConnectMailFolderAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        //}

        private ValueTask MessageArrival(IList<IMessageSummary> messages) => ValueTask.CompletedTask;

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