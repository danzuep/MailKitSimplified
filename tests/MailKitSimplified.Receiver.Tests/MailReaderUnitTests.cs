using MailKitSimplified.Receiver.Abstractions;
using MailKitSimplified.Receiver.Services;

namespace MailKitSimplified.Receiver.Tests
{
    public class MailReaderUnitTests
    {
        private readonly Mock<IMailFolder> _mailFolderMock = new Mock<IMailFolder>();
        private readonly Mock<IImapReceiver> _imapReceiverMock = new Mock<IImapReceiver>();
        private readonly IMailReader _mailReader;

        public MailReaderUnitTests()
        {
            // Arrange
            _mailFolderMock.Setup(_ => _.OpenAsync(It.IsAny<FolderAccess>(), It.IsAny<CancellationToken>())).Verifiable();
            _mailFolderMock.Setup(_ => _.CloseAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>())).Verifiable();
            _imapReceiverMock.Setup(_ => _.ConnectMailFolderAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(_mailFolderMock.Object).Verifiable();
            _mailReader = new MailReader(_imapReceiverMock.Object).Skip(0).Take(1);
        }

        [Fact]
        public void ToString_ReturnsOverriddenToString()
        {
            var description = _mailReader.ToString();
            Assert.False(string.IsNullOrWhiteSpace(description));
            Assert.DoesNotContain(nameof(MailReader), description);
        }

        [Fact]
        public async Task GetMessageAsync_WithNullMessageSummary_ReturnsMimeMessages()
        {
            // Arrange
            _mailFolderMock.Setup(_ => _.GetMessageAsync(It.IsAny<int>(), It.IsAny<CancellationToken>(), It.IsAny<ITransferProgress>()))
                .ReturnsAsync(new MimeMessage()).Verifiable();
            // Act
            var mimeMessages = await _mailReader.GetMimeMessagesAsync(It.IsAny<CancellationToken>(), It.IsAny<ITransferProgress>());
            // Assert
            Assert.NotNull(mimeMessages);
        }

        [Fact]
        public async Task GetMessageSummariesAsync_WithMessageSummaryItemFilter_ReturnsMessageSummaries()
        {
            // Arrange
            var stubMessageSummaries = new List<IMessageSummary>();
            _mailFolderMock.Setup(_ => _.FetchAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<IFetchRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(stubMessageSummaries);
            // Act
            var messageSummaries = await _mailReader.GetMessageSummariesAsync(MessageSummaryItems.UniqueId, It.IsAny<CancellationToken>());
            // Assert
            Assert.NotNull(messageSummaries);
            Assert.Equal(stubMessageSummaries, messageSummaries);
        }

        [Fact]
        public async Task GetMessageSummariesAsync_ReturnsMessageSummaries()
        {
            // Arrange
            var stubMessageSummaries = new List<IMessageSummary>();
            _mailFolderMock.Setup(_ => _.FetchAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<IFetchRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(stubMessageSummaries);
            // Act
            var messageSummaries = await _mailReader.GetMessageSummariesAsync(It.IsAny<CancellationToken>());
            // Assert
            Assert.NotNull(messageSummaries);
            Assert.Equal(stubMessageSummaries, messageSummaries);
        }
    }
}