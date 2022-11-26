using MailKitSimplified.Receiver.Abstractions;
using MailKitSimplified.Receiver.Services;
using MimeKit;

namespace MailKitSimplified.Receiver.Tests
{
    public class MailFolderReaderUnitTests
    {
        private readonly Mock<IMailFolder> _mailFolderMock = new();
        private readonly Mock<IMailFolderClient> _mailFolderClientMock = new();
        private readonly Mock<IImapReceiver> _imapReceiverMock = new();
        private readonly IMailFolderReader _mailFolderReader;

        public MailFolderReaderUnitTests()
        {
            // Arrange
            _mailFolderClientMock.Setup(_ => _.ConnectAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(_mailFolderMock.Object).Verifiable();
            _imapReceiverMock.Setup(_ => _.ConnectMailFolderAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(_mailFolderMock.Object).Verifiable();
            _imapReceiverMock.Setup(_ => _.ConnectMailFolderClientAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(_mailFolderClientMock.Object).Verifiable();
            _mailFolderReader = new MailFolderReader(_imapReceiverMock.Object);
        }

        [Fact]
        public void ToString_VerifyMailFolderClientToStringCalled()
        {
            // Arrange
            _imapReceiverMock.Setup(_ => _.ToString());
            // Act
            _ = _mailFolderReader.ToString();
            // Assert / Verify
            _imapReceiverMock.Verify(_ => _.ToString(), Times.Once);
        }

        [Fact]
        public async Task GetMessageSummariesAsync_WithAnyUniqueIds_ReturnsMimeMessage()
        {
            // Arrange
            var stubMessageSummaries = new List<IMessageSummary>();
            _mailFolderMock.Setup(_ => _.FetchAsync(It.IsAny<IList<UniqueId>>(), It.IsAny<IFetchRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(stubMessageSummaries);
            // Act
            var messageSummaries = await _mailFolderReader.GetMessageSummariesAsync(new List<UniqueId>(), It.IsAny<MessageSummaryItems>(), It.IsAny<CancellationToken>());
            // Assert
            Assert.NotNull(messageSummaries);
            Assert.Equal(stubMessageSummaries, messageSummaries);
        }

        [Fact]
        public async Task GetMessageAsync_WithAnyUniqueId_ReturnsMimeMessage()
        {
            // Arrange
            var stubMimeMessage = Mock.Of<MimeMessage>();
            _mailFolderMock.Setup(_ => _.GetMessageAsync(It.IsAny<UniqueId>(), It.IsAny<CancellationToken>(), It.IsAny<ITransferProgress>()))
                .ReturnsAsync(stubMimeMessage).Verifiable();
            // Act
            var mimeMessage = await _mailFolderReader.GetMimeMessageAsync(It.IsAny<UniqueId>(), It.IsAny<CancellationToken>());
            // Assert
            Assert.NotNull(mimeMessage);
            Assert.Equal(stubMimeMessage, mimeMessage);
            _mailFolderMock.Verify(_ => _.GetMessageAsync(It.IsAny<UniqueId>(), It.IsAny<CancellationToken>(), It.IsAny<ITransferProgress>()), Times.Once);
        }

        [Fact]
        public async Task GetMimeMessagesAsync_WithAnyUniqueIds_ReturnsMimeMessages()
        {
            // Arrange
            var stubMimeMessage = Mock.Of<MimeMessage>();
            var expected = new List<MimeMessage> { stubMimeMessage };
            _mailFolderMock.Setup(_ => _.GetMessageAsync(It.IsAny<UniqueId>(), It.IsAny<CancellationToken>(), It.IsAny<ITransferProgress>()))
                .ReturnsAsync(stubMimeMessage).Verifiable();
            // Act
            var fakeUniqueIds = new List<UniqueId> { new UniqueId() };
            var mimeMessages = await _mailFolderReader.GetMimeMessagesAsync(fakeUniqueIds, It.IsAny<CancellationToken>());
            // Assert
            Assert.NotNull(mimeMessages);
            Assert.Equal(expected, mimeMessages);
            _mailFolderMock.Verify(_ => _.GetMessageAsync(It.IsAny<UniqueId>(), It.IsAny<CancellationToken>(), It.IsAny<ITransferProgress>()), Times.Once);
        }
    }
}