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
        private const int _defaultPort = 143;
        private readonly Mock<IImapClient> _imapClientMock = new();
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

        [Fact]
        public void CreateEmailReceiverOptions_WithInlineHostPort_ReturnsSplitHostPort()
        {
            var emailReceiverOptions = new EmailReceiverOptions($"{_localhost}:{_defaultPort}");
            Assert.NotNull(emailReceiverOptions);
            Assert.Equal(_localhost, emailReceiverOptions.ImapHost);
            Assert.Equal(_defaultPort, emailReceiverOptions.ImapPort);
        }

        [Theory]
        [InlineData(_localhost, _defaultPort)]
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
            Assert.IsAssignableFrom<IImapReceiver>(imapReceiver);
        }

        [Fact]
        public void CreateImapReceiver_WithAnyHostAndCredential_ReturnsImapReceiver()
        {
            using var imapReceiver = ImapReceiver.Create(_localhost, It.IsAny<NetworkCredential>());
            Assert.NotNull(imapReceiver);
            Assert.IsAssignableFrom<IImapReceiver>(imapReceiver);
        }

        [Fact]
        public void CreateImapReceiver_WithFluentMethods_ReturnsImapReceiver()
        {
            using var imapReceiver = ImapReceiver.Create(_localhost)
                .SetPort(It.IsAny<ushort>())
                .SetCredential(It.IsAny<string>(), It.IsAny<string>())
                .SetProtocolLog(It.IsAny<string>());
            Assert.NotNull(imapReceiver);
            Assert.IsAssignableFrom<IImapReceiver>(imapReceiver);
        }

        [Fact]
        public async Task DisposeAsync_WithImapReceiver()
        {
            var imapReceiver = ImapReceiver.Create(_localhost);
            await imapReceiver.DisposeAsync();
            Assert.IsAssignableFrom<IImapReceiver>(imapReceiver);
        }

        [Fact]
        public async Task ConnectImapClientAsync_VerifyType()
        {
            // Act
            var imapReceiver = await _imapReceiver.ConnectImapClientAsync(It.IsAny<CancellationToken>());
            // Assert
            Assert.NotNull(imapReceiver);
            Assert.IsAssignableFrom<IImapClient>(imapReceiver);
            _imapClientMock.Verify(_ => _.ConnectAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<SecureSocketOptions>(), It.IsAny<CancellationToken>()), Times.Once);
            _imapClientMock.Verify(_ => _.AuthenticateAsync(It.IsAny<ICredentials>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ConnectMailFolderAsync_VerifyType()
        {
            // Act
            var mailFolder = await _imapReceiver.ConnectMailFolderAsync(It.IsAny<CancellationToken>());
            // Assert
            Assert.NotNull(mailFolder);
            Assert.IsAssignableFrom<IMailFolder>(mailFolder);
        }

        [Fact]
        public async Task ConnectMailFolderClientAsync_VerifyType()
        {
            // Act
            var mailFolderClient = await _imapReceiver.ConnectMailFolderClientAsync(It.IsAny<CancellationToken>());
            // Assert
            Assert.NotNull(mailFolderClient);
            Assert.IsAssignableFrom<IMailFolderClient>(mailFolderClient);
        }

        [Fact]
        public void ReadMail_WithImapReceiver_VerifyType()
        {
            var mailFolderReader = _imapReceiver.ReadMail;
            Assert.NotNull(mailFolderReader);
            Assert.IsAssignableFrom<IMailFolderReader>(mailFolderReader);
        }

        [Fact]
        public void ReadFrom_WithAnyMailFolderName_VerifyType()
        {
            var mailFolderReader = _imapReceiver.ReadFrom("INBOX");
            Assert.NotNull(mailFolderReader);
            Assert.IsAssignableFrom<IMailFolderReader>(mailFolderReader);
        }

        [Fact]
        public void Folder_WithAnyMailFolderName_VerifyType()
        {
            var idleClient = _imapReceiver.Folder("INBOX");
            Assert.NotNull(idleClient);
            Assert.IsAssignableFrom<IIdleClientReceiver>(idleClient);
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
            Assert.IsAssignableFrom<IList<string>>(mailFolderNames);
            _imapClientMock.Verify(_ => _.GetFoldersAsync(It.IsAny<FolderNamespace>(), It.IsAny<StatusItems>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [Fact]
        public void ToString_Verify()
        {
            var description = _imapReceiver.ToString();
            Assert.Contains(_localhost, description);
        }
    }
}