global using Xunit;
using MailKitSimplified.Receiver.Services;
using MimeKit;
using System.Net;

namespace MailKitSimplified.Receiver.Tests
{
    public class MailKitSimplifiedReceiverUnitTests
    {
        private const string _imapHost = "imap.example.com";

        [Theory]
        [InlineData(_imapHost)]
        public void CreateImapClientService_WithAnyHost_ReturnsImapClientService(string imapHost)
        {
            using var imapClient = ImapClientService.Create(imapHost, new NetworkCredential());
            Assert.NotNull(imapClient);
        }

        //[Theory]
        //[InlineData(_imapHost)]
        //public async Task GetMessageAsync_WithAnyHost_ReturnsMimeMessage(string imapHost)
        //{
        //    using var imapClient = ImapClientService.Create(imapHost, new NetworkCredential());
        //    using var mailFolderReader = new MailFolderReader(imapClient);
        //    var mimeMessage = await mailFolderReader.GetMessageAsync();
        //    Assert.NotNull(mimeMessage);
        //}

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