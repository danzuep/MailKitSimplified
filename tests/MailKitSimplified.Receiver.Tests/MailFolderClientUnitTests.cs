using MailKit;
using MailKit.Search;
using MailKitSimplified.Receiver.Abstractions;
using MailKitSimplified.Receiver.Services;

namespace MailKitSimplified.Receiver.Tests
{
    public class MailFolderClientUnitTests
    {
        private readonly Mock<IMailFolder> _mailFolderMock = new();
        private readonly Mock<IImapReceiver> _imapReceiverMock = new();
        private readonly MailFolderClient _mailFolderClient;

        public MailFolderClientUnitTests()
        {
            // Arrange
            _mailFolderMock.Setup(_ => _.OpenAsync(It.IsAny<FolderAccess>(), It.IsAny<CancellationToken>())).Verifiable();
            _mailFolderMock.Setup(_ => _.CloseAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>())).Verifiable();
            _imapReceiverMock.Setup(_ => _.ConnectMailFolderAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(_mailFolderMock.Object).Verifiable();
            _mailFolderClient = new MailFolderClient(_imapReceiverMock.Object);
        }

        [Fact]
        public void Dispose_UsingMailFolderClient()
        {
            using var mailFolderClient = new MailFolderClient(_imapReceiverMock.Object);
            mailFolderClient.Dispose();
            Assert.NotNull(mailFolderClient);
            Assert.IsAssignableFrom<IMailFolderClient>(mailFolderClient);
        }

        [Fact]
        public async Task DisposeAsync_WithMailFolderClient()
        {
            using var mailFolderClient = new MailFolderClient(_imapReceiverMock.Object);
            await mailFolderClient.DisposeAsync();
            Assert.IsAssignableFrom<IMailFolderClient>(mailFolderClient);
        }

        [Fact]
        public void Copy_ReturnsMailFolderClient()
        {
            // Act
            var mailFolderClient = _mailFolderClient.Copy();
            // Assert
            Assert.NotNull(mailFolderClient);
            Assert.IsAssignableFrom<IMailFolderClient>(mailFolderClient);
        }

        [Fact]
        public void ToString_VerifyMailFolderClientToStringCalled()
        {
            // Act
            var description = _mailFolderClient.ToString();
            // Assert
            Assert.False(string.IsNullOrWhiteSpace(description));
            Assert.DoesNotContain(nameof(MailFolderClient), description);
        }

        [Fact]
        public async Task ConnectAsync_WithClosedMailFolder_ReturnsMailFolder()
        {
            // Act
            var mailFolder = await _mailFolderClient.ConnectAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>());
            // Assert
            Assert.NotNull(mailFolder);
            Assert.IsAssignableFrom<IMailFolder>(mailFolder);
        }

        [Fact]
        public async Task ConnectAsync_WithOpenMailFolder_ReturnsMailFolder()
        {
            // Arrange
            _mailFolderMock.SetupGet(_ => _.IsOpen).Returns(true).Verifiable();
            // Act
            var mailFolder = await _mailFolderClient.ConnectAsync(false, CancellationToken.None);
            // Assert
            Assert.NotNull(mailFolder);
            Assert.IsAssignableFrom<IMailFolder>(mailFolder);
        }

        [Fact]
        public async Task ConnectAsync_WithReadWriteAccess_ReturnsMailFolder()
        {
            // Arrange
            _mailFolderMock.SetupGet(_ => _.Access).Returns(FolderAccess.ReadOnly).Verifiable();
            // Act
            var mailFolder = await _mailFolderClient.ConnectAsync(true, CancellationToken.None);
            // Assert
            Assert.NotNull(mailFolder);
            Assert.IsAssignableFrom<IMailFolder>(mailFolder);
        }

        [Fact]
        public async Task SearchAsync_WithSingleQuery_ReturnsUniqueIds()
        {
            // Arrange
            _mailFolderMock.Setup(_ => _.SearchAsync(It.IsAny<SearchQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<UniqueId>()).Verifiable();
            // Act
            var uniqueIds = await _mailFolderClient.SearchAsync(SearchQuery.Recent, CancellationToken.None);
            // Assert
            Assert.NotNull(uniqueIds);
            Assert.IsAssignableFrom<IList<UniqueId>>(uniqueIds);
        }

        [Fact]
        public async Task SearchKeywordsAsync_ReturnsUniqueIds()
        {
            // Arrange
            _mailFolderMock.Setup(_ => _.SearchAsync(It.IsAny<SearchQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<UniqueId>()).Verifiable();
            // Act
            var uniqueIds = await _mailFolderClient.SearchKeywordsAsync(new List<string>(), CancellationToken.None);
            // Assert
            Assert.NotNull(uniqueIds);
            Assert.IsAssignableFrom<IList<UniqueId>>(uniqueIds);
        }

        [Fact]
        public async Task SearchBetweenDatesAsync_ReturnsUniqueIds()
        {
            // Arrange
            _mailFolderMock.Setup(_ => _.SearchAsync(It.IsAny<SearchQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<UniqueId>()).Verifiable();
            // Act
            var uniqueIds = await _mailFolderClient.SearchBetweenDatesAsync(DateTime.Now, null, CancellationToken.None);
            // Assert
            Assert.NotNull(uniqueIds);
            Assert.IsAssignableFrom<IList<UniqueId>>(uniqueIds);
        }
    }
}