global using Xunit;
global using Moq;
using System.Net;
using MailKitSimplified.Receiver.Services;
using MailKitSimplified.Receiver.Abstractions;
using MailKit;

namespace MailKitSimplified.Receiver.Tests
{
    public class MailKitSimplifiedReceiverUnitTests
    {
        private const string _imapHost = "imap.example.com";

        [Theory]
        [InlineData(_imapHost)]
        public void CreateImapClientService_WithAnyHost_ReturnsImapClientService(string imapHost)
        {
            using var imapClient = ImapReceiver.Create(imapHost, new NetworkCredential());
            Assert.NotNull(imapClient);
        }

        [Fact]
        public async Task GetMessageAsync_WithNullMessageSummary_ReturnsNull()
        {
            // Arrange
            var mailFolderMock = new Mock<IMailFolder>();
            var imapClientMock = new Mock<IImapReceiver>();
            using var mailFolderReader = new MailFolderReader(imapClientMock.Object);
            imapClientMock.Setup(_ => _.ConnectAsync(It.IsAny<CancellationToken>()))
                .Returns(ValueTask.FromResult(mailFolderMock.Object));
            imapClientMock.Setup(_ => _.GetFolderAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(ValueTask.FromResult(mailFolderMock.Object));
            // Act
            var mimeMessage = await mailFolderReader.GetMimeMessageAsync(null);
            // Assert
            Assert.Null(mimeMessage);
        }

        //[Theory]
        //[InlineData(_imapHost)]
        //public async Task GetRangeAsync_WithAnyHost_ReturnsMimeMessages(string imapHost)
        //{
        //    using var imapClient = ImapClientService.Create(imapHost, new NetworkCredential());
        //    using var mailFolderReader = new MailFolderReader(imapClient);
        //    var mimeMessages = await mailFolderReader.GetRangeAsync();
        //    Assert.NotNull(mimeMessages);
        //    Assert.True(mimeMessages.Any());
        //}

        //[Theory]
        //[InlineData(_imapHost)]
        //public async Task GetMimeMessagesAsync_WithAnyHost_ReturnsMimeMessages(string imapHost)
        //{
        //    using var imapClient = ImapClientService.Create(imapHost, new NetworkCredential());
        //    using var mailFolderReader = new MailFolderReader(imapClient);
        //    var messageSummaries = await mailFolderReader.FetchMessageSummariesAsync(0, 1);
        //    var mimeMessages = await mailFolderReader.GetMimeMessagesAsync(messageSummaries);
        //    Assert.NotNull(mimeMessages);
        //    Assert.True(mimeMessages.Any());
        //}

        //[Theory]
        //[InlineData(_imapHost)]
        //public async Task MonitorAsync_WithAnyHost_ReturnsMimeMessages(string imapHost)
        //{
        //    using var imapClient = ImapMonitorService.Create(imapHost, input, output);
        //    await imapClient.Inbox.MonitorAsync();
        //}
    }
}