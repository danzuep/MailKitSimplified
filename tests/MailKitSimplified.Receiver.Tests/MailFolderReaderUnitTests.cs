using MailKit.Search;
using MailKitSimplified.Receiver.Abstractions;
using MailKitSimplified.Receiver.Services;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;

namespace MailKitSimplified.Receiver.Tests
{
    public class MailFolderReaderUnitTests
    {
        private readonly List<IMessageSummary> _stubMessageSummaries = new();
        private readonly Mock<IMailFolder> _mailFolderMock = new();
        private readonly Mock<IImapReceiver> _imapReceiverMock = new();
        private readonly MailFolderReader _mailFolderReader;

        public MailFolderReaderUnitTests()
        {
            // Arrange
            var messageSummary = new MessageSummary((int)UniqueId.MinValue.Id);
            _stubMessageSummaries.Add(messageSummary);
            _mailFolderMock.Setup(_ => _.OpenAsync(It.IsAny<FolderAccess>(), It.IsAny<CancellationToken>())).Verifiable();
            _mailFolderMock.Setup(_ => _.CloseAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>())).Verifiable();
            _mailFolderMock.Setup(_ => _.FetchAsync(It.IsAny<IList<UniqueId>>(), It.IsAny<IFetchRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(_stubMessageSummaries).Verifiable();
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
            var range = new UniqueIdRange(UniqueId.MinValue, UniqueId.MinValue);
            // Act
            var messageSummaries = await _mailFolderReader.GetMessageSummariesAsync(range, It.IsAny<MessageSummaryItems>(), It.IsAny<CancellationToken>());
            // Assert
            Assert.NotNull(messageSummaries);
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
            var fakeUniqueIds = new List<UniqueId> { UniqueId.MinValue };
            var mimeMessages = await _mailFolderReader.GetMimeMessagesAsync(fakeUniqueIds, It.IsAny<CancellationToken>());
            // Assert
            Assert.NotNull(mimeMessages);
            Assert.Equal(expected, mimeMessages);
            _mailFolderMock.Verify(_ => _.GetMessageAsync(It.IsAny<UniqueId>(), It.IsAny<CancellationToken>(), It.IsAny<ITransferProgress>()), Times.Once);
        }

        [Fact]
        public async Task GetMimeMessagesAsync_SkipTakeItemsQuery_ReturnsMimeMessages()
        {
            // Arrange
            _mailFolderMock.Setup(_ => _.SearchAsync(It.IsAny<SearchQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Array.Empty<UniqueId>()).Verifiable();
            _mailFolderMock.Setup(_ => _.GetMessageAsync(It.IsAny<int>(), It.IsAny<CancellationToken>(), It.IsAny<ITransferProgress>()))
                .ReturnsAsync(new MimeMessage()).Verifiable();
            // Act
            var mimeMessages = await _mailFolderReader.Skip(0).Take(1, continuous: false)
                .Items(MailFolderReader.CoreMessageItems).Query(SearchQuery.NotSeen)
                .GetMimeMessagesAsync(It.IsAny<CancellationToken>(), It.IsAny<ITransferProgress>());
            // Assert
            Assert.NotNull(mimeMessages);
        }

        [Fact]
        public async Task GetMimeMessagesRangeAsync_ContinuousRange_ReturnsMimeMessages()
        {
            // Arrange
            _mailFolderMock.Setup(_ => _.GetMessageAsync(It.IsAny<UniqueId>(), It.IsAny<CancellationToken>(), It.IsAny<ITransferProgress>()))
                .ReturnsAsync(new MimeMessage()).Verifiable();
            var mailReader = _mailFolderReader.Range(UniqueId.MinValue, UniqueId.MinValue, continuous: true);
            // Act
            var mimeMessages = await mailReader.GetMimeMessagesAsync(It.IsAny<CancellationToken>(), It.IsAny<ITransferProgress>());
            // Assert
            Assert.Single(mimeMessages);
        }

        [Fact]
        public async Task GetMimeMessagesEnvelopeBodyAsync_Range_ReturnsMimeMessages()
        {
            // Arrange
            var stubMessageSummaries = new List<IMessageSummary>();
            _mailFolderMock.Setup(_ => _.FetchAsync(It.IsAny<IList<UniqueId>>(), It.IsAny<IFetchRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(stubMessageSummaries);
            _mailFolderMock.Setup(_ => _.GetMessageAsync(It.IsAny<UniqueId>(), It.IsAny<CancellationToken>(), It.IsAny<ITransferProgress>()))
                .ReturnsAsync(new MimeMessage()).Verifiable();
            // Act
            var mimeMessages = await _mailFolderReader.Range(UniqueId.MinValue, UniqueId.MinValue, continuous: false)
                .GetMimeMessagesEnvelopeBodyAsync(It.IsAny<CancellationToken>());
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