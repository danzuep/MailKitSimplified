using MailKitSimplified.Receiver.Abstractions;
using MailKitSimplified.Receiver.Services;

namespace MailKitSimplified.Receiver.Tests
{
    public class MailFolderClientUnitTests
    {
        private readonly Mock<IMailFolder> _mailFolderMock = new Mock<IMailFolder>();
        private readonly IMailFolderClient _mailFolderClient;

        public MailFolderClientUnitTests()
        {
            // Arrange
            _mailFolderMock.Setup(_ => _.OpenAsync(It.IsAny<FolderAccess>(), It.IsAny<CancellationToken>())).Verifiable();
            _mailFolderMock.Setup(_ => _.CloseAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>())).Verifiable();
            _mailFolderClient = new MailFolderClient(_mailFolderMock.Object);
        }

        [Fact]
        public void UsingMailFolderClient_ReturnsDisposableMailFolderClient()
        {
            using var mailFolderClient = new MailFolderClient(_mailFolderMock.Object);
            Assert.NotNull(mailFolderClient);
        }

        [Fact]
        public async Task ToStringDisposeAsync_ReturnsValidDescriptionAsync()
        {
            // Arrange
            var mailFolderClient = new MailFolderClient(_mailFolderMock.Object);
            // Act
            var description = mailFolderClient.ToString();
            await mailFolderClient.DisposeAsync();
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
        }
    }
}