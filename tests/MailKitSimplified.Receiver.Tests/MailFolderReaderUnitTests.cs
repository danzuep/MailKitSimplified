using MailKitSimplified.Receiver.Abstractions;
using MailKitSimplified.Receiver.Services;

namespace MailKitSimplified.Receiver.Tests
{
    public class MailFolderReaderUnitTests
    {
        private readonly Mock<IMailFolder> _mailFolderMock = new Mock<IMailFolder>();
        private readonly Mock<IMailFolderClient> _mailFolderClientMock = new Mock<IMailFolderClient>();
        private readonly IMailFolderReader _mailFolderReader;

        public MailFolderReaderUnitTests()
        {
            // Arrange
            _mailFolderClientMock.Setup(_ => _.ConnectAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(_mailFolderMock.Object).Verifiable();
            _mailFolderReader = new MailFolderReader(_mailFolderClientMock.Object);
        }

        [Fact]
        public void ToString_VerifyMailFolderClientToStringCalled()
        {
            // Arrange
            _mailFolderClientMock.Setup(_ => _.ToString());
            // Act
            _ = _mailFolderReader.ToString();
            // Assert / Verify
            _mailFolderClientMock.Verify(_ => _.ToString(), Times.Once);
        }

        [Fact]
        public void Dispose_UsingMailFolderReader()
        {
            using var mailFolderReader = new MailFolderReader(_mailFolderClientMock.Object);
            Assert.NotNull(mailFolderReader);
        }

        [Fact]
        public async Task DisposeAsync_WithNewMailFolderReaderAsync()
        {
            var mailFolderReader = new MailFolderReader(_mailFolderClientMock.Object);
            await mailFolderReader.DisposeAsync();
        }

        [Fact]
        public async Task ReconnectAsync_WithReadOnlyAccess_ReturnsMailFolder()
        {
            // Act
            var mailFolder = await _mailFolderReader.ReconnectAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>());
            // Assert
            Assert.NotNull(mailFolder);
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
            var stubMimeMessage = new MimeMessage();
            _mailFolderMock.Setup(_ => _.GetMessageAsync(It.IsAny<UniqueId>(), It.IsAny<CancellationToken>(), It.IsAny<ITransferProgress>()))
                .ReturnsAsync(stubMimeMessage).Verifiable();
            // Act
            var mimeMessage = await _mailFolderReader.GetMimeMessageAsync(It.IsAny<UniqueId>(), It.IsAny<CancellationToken>());
            // Assert
            Assert.NotNull(mimeMessage);
        }

        [Fact]
        public async Task GetMimeMessagesAsync_WithAnyUniqueIds_ReturnsMimeMessages()
        {
            // Arrange
            var stubMimeMessage = new MimeMessage();
            _mailFolderMock.Setup(_ => _.GetMessageAsync(It.IsAny<UniqueId>(), It.IsAny<CancellationToken>(), It.IsAny<ITransferProgress>()))
                .ReturnsAsync(stubMimeMessage).Verifiable();
            // Act
            var mimeMessages = await _mailFolderReader.GetMimeMessagesAsync(It.IsAny<IEnumerable<UniqueId>>(), It.IsAny<CancellationToken>());
            // Assert
            Assert.NotNull(mimeMessages);
        }
    }
}