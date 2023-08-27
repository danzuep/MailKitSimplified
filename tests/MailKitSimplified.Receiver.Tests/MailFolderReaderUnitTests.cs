using MailKit.Search;
using MailKitSimplified.Receiver.Abstractions;
using MailKitSimplified.Receiver.Services;

namespace MailKitSimplified.Receiver.Tests
{
    public class MailFolderReaderUnitTests
    {
        private readonly Mock<IMailFolder> _mailFolderMock = new();
        private readonly Mock<IImapReceiver> _imapReceiverMock = new();
        private readonly MailFolderReader _mailFolderReader;

        public MailFolderReaderUnitTests()
        {
            // Arrange
            _mailFolderMock.Setup(_ => _.OpenAsync(It.IsAny<FolderAccess>(), It.IsAny<CancellationToken>())).Verifiable();
            _mailFolderMock.Setup(_ => _.CloseAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>())).Verifiable();
            _imapReceiverMock.Setup(_ => _.ConnectMailFolderAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(_mailFolderMock.Object).Verifiable();
            _mailFolderReader = new MailFolderReader(_imapReceiverMock.Object);
        }

        [Fact]
        public void Dispose_UsingMailFolderReader()
        {
            using var mailFolderReader = new MailFolderReader(_imapReceiverMock.Object);
            mailFolderReader.Dispose();
            Assert.NotNull(mailFolderReader);
            Assert.IsAssignableFrom<IMailFolderReader>(mailFolderReader);
        }

        [Fact]
        public async Task DisposeAsync_WithMailFolderReader()
        {
            using var mailFolderReader = new MailFolderReader(_imapReceiverMock.Object);
            await mailFolderReader.DisposeAsync();
            Assert.IsAssignableFrom<IMailFolderReader>(mailFolderReader);
        }

        [Fact]
        public void Copy_ReturnsShallowCopy()
        {
            // Act
            var shallowCopy = _mailFolderReader.Copy();
            // Assert
            Assert.NotNull(shallowCopy);
            Assert.IsAssignableFrom<IMailFolderReader>(shallowCopy);
        }

        [Fact]
        public void ToString_ReturnsOverriddenToString()
        {
            // Act
            var description = _mailFolderReader.ToString();
            // Assert
            Assert.False(string.IsNullOrWhiteSpace(description));
            Assert.DoesNotContain(nameof(MailFolderReader), description);
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
        public async Task GetMessageSummariesAsync_WithUniqueIdRange_ReturnsMimeMessage()
        {
            var stubMessageSummaries = new List<IMessageSummary>();
            _mailFolderMock.Setup(_ => _.FetchAsync(It.IsAny<IList<UniqueId>>(), It.IsAny<IFetchRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(stubMessageSummaries);
            // Build uuid range to fetch
            var range = new UniqueIdRange(UniqueId.MinValue, UniqueId.MaxValue);
            // ToList() throws an overflow or out-of-memory exception:
            //new UniqueIdRange(UniqueId.MinValue, new UniqueId(int.MaxValue - 1)).ToList();
            // Serach inbox for messages uid
            var messageSummaries = await _mailFolderReader.GetMessageSummariesAsync(range);
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

        [Fact]
        public async Task GetMessageAsync_WithNullMessageSummary_ReturnsMimeMessages()
        {
            // Arrange
            _mailFolderMock.Setup(_ => _.SearchAsync(It.IsAny<SearchQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Array.Empty<UniqueId>()).Verifiable();
            _mailFolderMock.Setup(_ => _.GetMessageAsync(It.IsAny<int>(), It.IsAny<CancellationToken>(), It.IsAny<ITransferProgress>()))
                .ReturnsAsync(new MimeMessage()).Verifiable();
            // Act
            var mimeMessages = await _mailFolderReader.Skip(0).Take(1).Items(MailFolderReader.CoreMessageItems).Query(SearchQuery.NotSeen)
                .GetMimeMessagesAsync(It.IsAny<CancellationToken>(), It.IsAny<ITransferProgress>());
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
            var messageSummaries = await _mailFolderReader.GetMessageSummariesAsync(MessageSummaryItems.UniqueId, It.IsAny<CancellationToken>());
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
            var messageSummaries = await _mailFolderReader.GetMessageSummariesAsync(It.IsAny<CancellationToken>());
            // Assert
            Assert.NotNull(messageSummaries);
            Assert.Equal(stubMessageSummaries, messageSummaries);
        }
    }
}