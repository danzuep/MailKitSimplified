using MailKitSimplified.Receiver.Abstractions;
using MailKitSimplified.Receiver.Services;

namespace MailKitSimplified.Receiver.Tests
{
    public class MailFolderReaderUnitTests
    {
        private readonly Mock<IMailFolder> _mailFolderMock = new Mock<IMailFolder>();
        private readonly IMailFolderReader _mailFolderReader;

        public MailFolderReaderUnitTests()
        {
            // Arrange
            _mailFolderMock.Setup(_ => _.OpenAsync(It.IsAny<FolderAccess>(), It.IsAny<CancellationToken>())).Verifiable();
            _mailFolderMock.Setup(_ => _.CloseAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>())).Verifiable();
            _mailFolderReader = new MailFolderReader(_mailFolderMock.Object);
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