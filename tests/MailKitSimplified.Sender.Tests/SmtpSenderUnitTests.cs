global using Xunit;
global using Moq;
global using MimeKit;
global using MailKit;
global using MailKit.Net.Smtp;
using MailKit.Security;
using System.Net;
using System.IO.Abstractions.TestingHelpers;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MailKitSimplified.Sender.Services;
using MailKitSimplified.Sender.Models;
using MailKitSimplified.Sender.Abstractions;

namespace MailKitSimplified.Sender.Tests
{
    public class SmtpSenderUnitTests
    {
        private const string _logFilePath = @"Logs\SmtpClient.txt";
        private const string _localhost = "localhost";
        private const int _defaultPort = 25;
        private readonly MockFileSystem _fileSystem = new();
        private readonly Mock<ISmtpClient> _smtpClientMock = new();
        private readonly SmtpSender _smtpSender;

        public SmtpSenderUnitTests()
        {
            // Arrange
            var loggerMock = Mock.Of<ILogger<SmtpSender>>();
            var protocolLoggerMock = Mock.Of<IProtocolLogger>();
            _smtpClientMock.Setup(_ => _.ConnectAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<SecureSocketOptions>(), It.IsAny<CancellationToken>())).Verifiable();
            _smtpClientMock.Setup(_ => _.AuthenticateAsync(It.IsAny<ICredentials>(), It.IsAny<CancellationToken>())).Verifiable();
            _smtpClientMock.SetupGet(_ => _.AuthenticationMechanisms).Returns(new HashSet<string>()).Verifiable();
            _smtpClientMock.Setup(_ => _.AuthenticateAsync(It.IsAny<SaslMechanism>(), It.IsAny<CancellationToken>())).Verifiable();
            _smtpClientMock.Setup(_ => _.SendAsync(It.IsAny<MimeMessage>(), It.IsAny<CancellationToken>(), It.IsAny<ITransferProgress>()))
                .ReturnsAsync("Mail accepted").Verifiable();
            var smtpSenderOptions = Options.Create(new EmailSenderOptions(_localhost, new NetworkCredential()));
            _smtpSender = new SmtpSender(smtpSenderOptions, loggerMock, protocolLoggerMock, _smtpClientMock.Object, NullLoggerFactory.Instance);
        }

        [Fact]
        public void SmtpClient_VerifyType()
        {
            // Act
            using var smtpClient = _smtpSender.SmtpClient;
            // Assert
            Assert.NotNull(smtpClient);
            Assert.IsAssignableFrom<ISmtpClient>(smtpClient);
        }

        [Fact]
        public void WriteEmail_VerifyType()
        {
            // Act
            var emailWriter = _smtpSender.WriteEmail;
            // Assert
            Assert.NotNull(emailWriter);
            Assert.IsAssignableFrom<IEmailWriter>(emailWriter);
        }

        [Fact]
        public void CreateEmailSenderOptions_WithInlineHostPort_ReturnsSplitHostPort()
        {
            var emailSenderOptions = new EmailSenderOptions($"{_localhost}:{_defaultPort}");
            Assert.NotNull(emailSenderOptions);
            Assert.Equal(_localhost, emailSenderOptions.SmtpHost);
            Assert.Equal(_defaultPort, emailSenderOptions.SmtpPort);
        }

        [Fact]
        public void CreateEmailSender_WithExistingClient_ReturnsSender()
        {
            var emailSenderOptions = new EmailSenderOptions(_localhost);
            using var smtpSender = SmtpSender.Create(Mock.Of<ISmtpClient>(), emailSenderOptions);
            Assert.NotNull(smtpSender);
            Assert.IsAssignableFrom<ISmtpSender>(smtpSender);
            Assert.Equal(_localhost, emailSenderOptions.SmtpHost);
        }

        [Theory]
        [InlineData(_localhost, _defaultPort)]
        [InlineData("smtp.example.com", 0)]
        [InlineData("smtp.google.com", 465)]
        [InlineData("smtp.sendgrid.com", 2525)]
        [InlineData("smtp.mail.yahoo.com", 587)]
        [InlineData("outlook.office365.com", ushort.MinValue)]
        [InlineData("smtp.freesmtpservers.com", ushort.MaxValue)]
        public void CreateSmtpSender_WithValidSmtpHostNames_ReturnsSmtpSender(string smtpHost, ushort smtpPort)
        {
            using var smtpSender = SmtpSender.Create(smtpHost, smtpPort);
            Assert.NotNull(smtpSender);
            Assert.IsAssignableFrom<ISmtpSender>(smtpSender);
        }

        [Fact]
        public void CreateSmtpSender_WithAnyHostAndCredential_ReturnsSmtpSender()
        {
            using var smtpSender = SmtpSender.Create(_localhost, It.IsAny<NetworkCredential>());
            Assert.NotNull(smtpSender);
            Assert.IsAssignableFrom<ISmtpSender>(smtpSender);
        }

        [Fact]
        public void CreateSmtpSender_WithFluentMethods_ReturnsSmtpSender()
        {
            // Act
            using var smtpSender = SmtpSender.Create(_localhost)
                .SetPort(It.IsAny<ushort>(), It.IsAny<SecureSocketOptions>())
                .SetCredential(It.IsAny<string>(), It.IsAny<string>())
                .SetCustomAuthentication(It.IsAny<Func<ISmtpClient, Task>>())
                .SetSmtpClient(It.IsAny<ISmtpClient>())
                .SetProtocolLog(It.IsAny<IProtocolLogger>())
                .SetProtocolLog(It.IsAny<string>())
                .SetLogger(It.IsAny<ILoggerFactory>(), It.IsAny<ILogger<SmtpSender>>())
                .RemoveCapabilities(It.IsAny<SmtpCapabilities>());
            smtpSender.RemoveAuthenticationMechanism("XOAUTH2");
            // Assert
            Assert.NotNull(smtpSender);
            Assert.IsAssignableFrom<ISmtpSender>(smtpSender);
        }

        [Fact]
        public async Task DisposeAsync_WithSmtpSender()
        {
            var smtpSender = SmtpSender.Create(_localhost);
            await smtpSender.DisposeAsync();
            Assert.IsAssignableFrom<ISmtpSender>(smtpSender);
        }

        [Fact]
        public async Task ConnectSmtpClientAsync_VerifyCalls()
        {
            // Act
            var smtpClient = await _smtpSender.ConnectSmtpClientAsync(It.IsAny<CancellationToken>());
            // Assert
            Assert.NotNull(smtpClient);
            Assert.IsAssignableFrom<ISmtpClient>(smtpClient);
            _smtpClientMock.Verify(_ => _.ConnectAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<SecureSocketOptions>(), It.IsAny<CancellationToken>()), Times.Once);
            _smtpClientMock.Verify(_ => _.AuthenticateAsync(It.IsAny<ICredentials>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public void GetProtocolLogger_WithAppend_VerifyType()
        {
            _fileSystem.AddFile(_logFilePath, new MockFileData(string.Empty));
            using var protocolLogger = new EmailSenderOptions
            {
                ProtocolLog = _logFilePath,
                ProtocolLogFileAppend = true
            }.CreateProtocolLogger(_fileSystem);
            Assert.NotNull(protocolLogger);
            Assert.IsAssignableFrom<IProtocolLogger>(protocolLogger);
        }

        [Fact]
        public void GetProtocolLogger_WithCreate_VerifyType()
        {
            using var protocolLogger = new EmailSenderOptions
            {
                ProtocolLog = _logFilePath,
                ProtocolLogFileAppend = false
            }.CreateProtocolLogger(_fileSystem);
            Assert.NotNull(protocolLogger);
            Assert.IsAssignableFrom<IProtocolLogger>(protocolLogger);
        }

        [Fact]
        public async Task SendAsync_VerifySentAsync()
        {
            // Act
            await _smtpSender.SendAsync(new MimeMessage(), It.IsAny<CancellationToken>(), It.IsAny<ITransferProgress>());
            // Assert
            _smtpClientMock.Verify(_ => _.SendAsync(It.IsAny<MimeMessage>(), It.IsAny<CancellationToken>(), It.IsAny<ITransferProgress>()), Times.Once);
        }

        [Fact]
        public async Task TrySendAsync_VerifySentAsync()
        {
            var isSent = await _smtpSender.TrySendAsync(new MimeMessage(), It.IsAny<CancellationToken>(), It.IsAny<ITransferProgress>());
            Assert.True(isSent);
            _smtpClientMock.Verify(_ => _.SendAsync(It.IsAny<MimeMessage>(), It.IsAny<CancellationToken>(), It.IsAny<ITransferProgress>()), Times.Once);
        }

        [Fact]
        public void ValidateEmailAddresses_WithValidEmails_VerifyValid()
        {
            var source = new string[] { "from@localhost" };
            var destination = new string[] { "to@localhost" };
            var valid = SmtpSender.ValidateEmailAddresses(source, destination, NullLogger.Instance);
            Assert.True(valid);
        }

        [Fact]
        public void ValidateEmailAddresses_WithInvalidEmails_VerifyInvalid()
        {
            var source = new string[] { "from@localhost", "admin@localhost", "me" };
            var destination = new string[] { "to@localhost", "admin@localhost", "you" };
            var valid = SmtpSender.ValidateEmailAddresses(source, destination, NullLogger.Instance);
            Assert.False(valid);
        }

        [Fact]
        public void ValidateEmailAddresses_WithNoEmails_VerifyInvalid()
        {
            var valid = SmtpSender.ValidateEmailAddresses(new string[] { }, new string[] { }, NullLogger.Instance);
            Assert.False(valid);
        }

        [Fact]
        public void Copy_VerifyReturnsShallowCopy()
        {
            // Act
            var shallowCopy = _smtpSender.Copy();
            // Assert
            Assert.NotNull(shallowCopy);
            Assert.IsAssignableFrom<ISmtpSender>(shallowCopy);
        }

        [Fact]
        public void ToString_Verify()
        {
            var serialised = _smtpSender.ToString();
            Assert.Contains(_localhost, serialised);
        }
    }
}