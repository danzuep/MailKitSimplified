global using Xunit;
global using Moq;
global using MimeKit;
global using MailKit;
global using MailKit.Net.Imap;
using MailKit.Security;
using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MailKitSimplified.Receiver.Services;
using MailKitSimplified.Receiver.Abstractions;
using MailKitSimplified.Receiver.Models;

namespace MailKitSimplified.Receiver.Tests
{
    public class ImapReceiverUnitTests
    {
        private const string _localhost = "localhost";
        private static readonly string _inbox = "INBOX";
        private readonly Mock<IImapClient> _imapClientMock = new Mock<IImapClient>();
        private readonly IImapReceiver _imapReceiver;

        public ImapReceiverUnitTests()
        {
            // Arrange
            var loggerMock = new Mock<ILogger<ImapReceiver>>();
            var protocolLoggerMock = new Mock<IProtocolLogger>();
            _imapClientMock.Setup(_ => _.ConnectAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<SecureSocketOptions>(), It.IsAny<CancellationToken>())).Verifiable();
            _imapClientMock.Setup(_ => _.AuthenticateAsync(It.IsAny<ICredentials>(), It.IsAny<CancellationToken>())).Verifiable();
            _imapClientMock.SetupGet(_ => _.AuthenticationMechanisms).Returns(new HashSet<string>()).Verifiable();
            _imapClientMock.Setup(_ => _.AuthenticateAsync(It.IsAny<SaslMechanism>(), It.IsAny<CancellationToken>())).Verifiable();
            _imapClientMock.SetupGet(_ => _.Inbox).Returns(Mock.Of<IMailFolder>()).Verifiable();
            var imapReceiverOptions = Options.Create(new EmailReceiverOptions(_localhost, new NetworkCredential()));
            _imapReceiver = new ImapReceiver(imapReceiverOptions, loggerMock.Object, protocolLoggerMock.Object, _imapClientMock.Object);
        }

        [Theory]
        [InlineData(_localhost, 143)]
        [InlineData("imap.example.com", 0)]
        [InlineData("imap.google.com", 993)]
        [InlineData("imap.sendgrid.com", 995)]
        [InlineData("imap.mail.yahoo.com", 110)]
        [InlineData("outlook.office365.com", ushort.MinValue)]
        [InlineData("imap.freesmtpservers.com", ushort.MaxValue)]
        public void CreateImapReceiver_WithAnyHost_ReturnsImapReceiver(string imapHost, ushort imapPort)
        {
            using var imapReceiver = ImapReceiver.Create(imapHost, imapPort);
            Assert.NotNull(imapReceiver);
        }

        [Fact]
        public void CreateImapReceiver_WithAnyHostAndCredential_ReturnsImapReceiver()
        {
            using var imapReceiver = ImapReceiver.Create(_localhost, It.IsAny<NetworkCredential>());
            Assert.NotNull(imapReceiver);
        }

        [Fact]
        public async Task ConnectImapClientAsync_VerifyNotNull()
        {
            // Act
            var imapReceiver = await _imapReceiver.ConnectImapClientAsync(It.IsAny<CancellationToken>());
            // Assert
            Assert.NotNull(imapReceiver);
            _imapClientMock.Verify(_ => _.ConnectAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<SecureSocketOptions>(), It.IsAny<CancellationToken>()), Times.Once);
            _imapClientMock.Verify(_ => _.AuthenticateAsync(It.IsAny<ICredentials>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ConnectMailFolderAsync_VerifyNotNull()
        {
            // Act
            var mailFolder = await _imapReceiver.ConnectMailFolderAsync(_inbox, It.IsAny<CancellationToken>());
            // Assert
            Assert.NotNull(mailFolder);
        }

        [Fact]
        public async Task ConnectMailFolderClientAsync_VerifyNotNull()
        {
            // Act
            var mailFolder = await _imapReceiver.ConnectMailFolderClientAsync(_inbox, It.IsAny<bool>(), It.IsAny<CancellationToken>());
            // Assert
            Assert.NotNull(mailFolder);
        }

        [Fact]
        public void ReadMail_WithImapReceiver_VerifyNotNull()
        {
            Assert.NotNull(_imapReceiver.ReadMail);
        }

        [Fact]
        public void ReadFrom_WithAnyMailFolderName_VerifyNotNull()
        {
            Assert.NotNull(_imapReceiver.ReadFrom("INBOX"));
        }

        [Fact]
        public async Task GetMailFolderNamesAsync_VerifyCalls()
        {
            // Arrange
            var folderNamespaceStub = new FolderNamespaceCollection { new FolderNamespace('/', "") };
            _imapClientMock.SetupGet(_ => _.PersonalNamespaces).Returns(new FolderNamespaceCollection());
            _imapClientMock.SetupGet(_ => _.SharedNamespaces).Returns(folderNamespaceStub);
            _imapClientMock.SetupGet(_ => _.OtherNamespaces).Returns(folderNamespaceStub);
            _imapClientMock.Setup(_ => _.GetFoldersAsync(It.IsAny<FolderNamespace>(), It.IsAny<StatusItems>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Array.Empty<IMailFolder>()).Verifiable();
            // Act
            var mailFolderNames = await _imapReceiver.GetMailFolderNamesAsync(It.IsAny<CancellationToken>());
            // Assert
            Assert.NotNull(mailFolderNames);
            _imapClientMock.Verify(_ => _.GetFoldersAsync(It.IsAny<FolderNamespace>(), It.IsAny<StatusItems>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [Fact]
        public void ToString_Verify()
        {
            var serialised = _imapReceiver.ToString();
            Assert.Contains(_localhost, serialised);
        }

        //[Theory]
        //[InlineData(_localhost)]
        //public async Task MonitorAsync_WithAnyHost_ReturnsMimeMessages(string imapHost)
        //{
        //    using var imapClient = ImapMonitorService.Create(imapHost, input, output);
        //    await imapClient.Inbox.MonitorAsync();
        //}
    }
}