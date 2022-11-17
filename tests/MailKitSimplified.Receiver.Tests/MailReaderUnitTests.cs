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
        public void ToString_VerifyContainsSkipTake()
        {
            var mailReaderSerialised = _mailReader.ToString();
            Assert.Contains("skip ", mailReaderSerialised);
            Assert.Contains("take ", mailReaderSerialised);
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
        public async Task GetMessageSummariesAsync_WithMessageSummaryItemFilter_ReturnsNull()
        {
            // Arrange
            IList<IMessageSummary> stubMessageSummaries = Mock.Of<IList<IMessageSummary>>();
            //_mailFolderMock.Setup(_ => _.FetchAsync(It.IsAny<int>(), It.IsAny<int>(), MessageSummaryItems.UniqueId, It.IsAny<CancellationToken>()))
            //    .ReturnsAsync(stubMessageSummaries).Verifiable();
            // Act
            var mimeMessages = await _mailReader.GetMessageSummariesAsync(MessageSummaryItems.UniqueId, It.IsAny<CancellationToken>());
            // Assert
            Assert.Null(mimeMessages);
        }

        [Fact]
        public async Task GetMessageSummariesAsync_ReturnsNull()
        {
            // Act
            var mimeMessages = await _mailReader.GetMessageSummariesAsync(It.IsAny<CancellationToken>());
            // Assert
            Assert.Null(mimeMessages);
        }
    }
}