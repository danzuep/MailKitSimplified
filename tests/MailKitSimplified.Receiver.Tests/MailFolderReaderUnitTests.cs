using MailKit.Search;
using MailKitSimplified.Receiver.Abstractions;
using MailKitSimplified.Receiver.Extensions;
using MailKitSimplified.Receiver.Services;

namespace MailKitSimplified.Receiver.Tests
{
    public class MailFolderReaderUnitTests
    {
        private readonly List<IMessageSummary> _stubMessageSummaries = new()
        {
            new MessageSummary(Mock.Of<IMailFolder>(), (int)UniqueId.MinValue.Id)
        };
        private readonly UniqueId[] _stubUniqueIds = new UniqueId[] { UniqueId.MinValue };
        private readonly MimeMessage _stubMimeMessage = new();
        private readonly Mock<IMailFolder> _mailFolderMock = new();
        private readonly Mock<IImapReceiver> _imapReceiverMock = new();
        private readonly MailFolderReader _mailFolderReader;

        public MailFolderReaderUnitTests()
        {
            // Arrange
            _mailFolderMock.Setup(_ => _.OpenAsync(It.IsAny<FolderAccess>(), It.IsAny<CancellationToken>())).Verifiable();
            _mailFolderMock.Setup(_ => _.CloseAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>())).Verifiable();
            _mailFolderMock.Setup(_ => _.FetchAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<IFetchRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(_stubMessageSummaries).Verifiable();
            _mailFolderMock.Setup(_ => _.FetchAsync(It.IsAny<IList<UniqueId>>(), It.IsAny<IFetchRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(_stubMessageSummaries).Verifiable();
            _mailFolderMock.Setup(_ => _.GetMessageAsync(It.IsAny<int>(), It.IsAny<CancellationToken>(), It.IsAny<ITransferProgress>()))
                .ReturnsAsync(_stubMimeMessage).Verifiable();
            _mailFolderMock.Setup(_ => _.GetMessageAsync(It.IsAny<UniqueId>(), It.IsAny<CancellationToken>(), It.IsAny<ITransferProgress>()))
                .ReturnsAsync(_stubMimeMessage).Verifiable();
            _mailFolderMock.Setup(_ => _.SearchAsync(It.IsAny<IList<UniqueId>>(), It.IsAny<SearchQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(_stubUniqueIds).Verifiable();
            _mailFolderMock.Setup(_ => _.SearchAsync(It.IsAny<SearchQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(_stubUniqueIds).Verifiable();
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
        public async Task GetMessageSummariesAsync_WithMessageSummaryItemFilter_ReturnsMessageSummaries()
        {
            // Act
            var messageSummaries = await _mailFolderReader.Items(MessageSummaryItems.UniqueId)
                .GetMessageSummariesAsync(It.IsAny<CancellationToken>());
            // Assert
            Assert.NotNull(messageSummaries);
            Assert.Equal(_stubMessageSummaries, messageSummaries);
        }

        [Fact]
        public async Task GetMessageSummariesAsync_CoreMessageItems_ReturnsMessageSummaries()
        {
            // Act
            var messageSummaries = await _mailFolderReader.ItemsForMimeMessages()
                .GetMessageSummariesAsync(It.IsAny<CancellationToken>());
            // Assert
            Assert.NotNull(messageSummaries);
            Assert.Equal(_stubMessageSummaries, messageSummaries);
        }

        [Fact]
        public async Task GetMessageSummariesAsync_WithUniqueId_ReturnsMessageSummaries()
        {
            // Act
            var messageSummaries = await _mailFolderReader.Range(UniqueId.MinValue, batchSize: 0, continuous: true)
                .Items(It.IsAny<MessageSummaryItems>())
                .GetMessageSummariesAsync(It.IsAny<CancellationToken>());
            // Assert
            Assert.NotNull(messageSummaries);
            Assert.Equal(_stubMessageSummaries, messageSummaries);
            _mailFolderMock.Verify(_ => _.FetchAsync(It.IsAny<IList<UniqueId>>(), It.IsAny<IFetchRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetMessageAsync_WithUniqueId_ReturnsMimeMessage()
        {
            // Act
            var mimeMessages = await _mailFolderReader.Range(UniqueId.MinValue)
                .GetMimeMessagesAsync(It.IsAny<CancellationToken>());
            var mimeMessage = mimeMessages.FirstOrDefault();
            // Assert
            Assert.NotNull(mimeMessage);
            Assert.Equal(_stubMimeMessage, mimeMessage);
            _mailFolderMock.Verify(_ => _.GetMessageAsync(It.IsAny<UniqueId>(), It.IsAny<CancellationToken>(), It.IsAny<ITransferProgress>()), Times.Once);
        }

        [Fact]
        public async Task GetMimeMessagesAsync_WithAnyUniqueIds_ReturnsMimeMessages()
        {
            // Arrange
            var expected = new MimeMessage[] { _stubMimeMessage };
            // Act
            var mimeMessages = await _mailFolderReader.Range(UniqueId.MinValue)
                .GetMimeMessagesAsync(It.IsAny<CancellationToken>());
            // Assert
            Assert.NotNull(mimeMessages);
            Assert.Equal(expected, mimeMessages);
            _mailFolderMock.Verify(_ => _.GetMessageAsync(It.IsAny<UniqueId>(), It.IsAny<CancellationToken>(), It.IsAny<ITransferProgress>()), Times.Once);
        }

        [Fact]
        public async Task GetMimeMessagesAsync_SkipTakeItemsQuery_ReturnsMimeMessages()
        {
            // Act
            var mimeMessages = await _mailFolderReader.Skip(0)
                .Take(1, continuous: false)
                .Query(SearchQuery.NotSeen)
                .GetMimeMessagesAsync(It.IsAny<CancellationToken>(), It.IsAny<ITransferProgress>());
            // Assert
            Assert.NotNull(mimeMessages);
        }

        [Fact]
        public async Task GetMimeMessagesRangeAsync_ContinuousRange_ReturnsMimeMessages()
        {
            // Arrange
            var mailReader = _mailFolderReader.Range(UniqueId.MinValue, batchSize: 0, continuous: true);
            // Act
            var mimeMessages = await mailReader.GetMimeMessagesAsync(It.IsAny<CancellationToken>(), It.IsAny<ITransferProgress>());
            // Assert
            Assert.Single(mimeMessages);
        }

        [Fact]
        public async Task GetMimeMessagesEnvelopeBodyAsync_Range_ReturnsMimeMessages()
        {
            // Arrange
            var mailReader = _mailFolderReader.Range(UniqueId.MinValue, UniqueId.MinValue, continuous: false);
            // Act
            var mimeMessages = await mailReader.GetMimeMessagesEnvelopeBodyAsync(It.IsAny<CancellationToken>());
            // Assert
            Assert.NotNull(mimeMessages);
        }
    }
}