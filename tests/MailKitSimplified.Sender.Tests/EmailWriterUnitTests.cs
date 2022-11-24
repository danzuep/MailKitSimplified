using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using Microsoft.Extensions.Logging;
using MailKitSimplified.Sender.Abstractions;
using MailKitSimplified.Sender.Services;

namespace MailKitSimplified.Sender.Tests
{
    public class EmailWriterUnitTests
    {
        private const string _attachment1Path = @"C:\Temp\attachment1.txt";
        private const string _attachment2Path = @"C:\Temp\attachment2.pdf";

        private readonly IFileSystem _fileSystem;
        private readonly ILoggerFactory _loggerFactory;
        private readonly Mock<ISmtpSender> _smtpSenderMock = new();
        private readonly IEmailWriter _testEmail;

        public EmailWriterUnitTests()
        {
            // Arrange
            _fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { _attachment1Path, new MockFileData("ABC") },
                { _attachment2Path, new MockFileData("123") }
            });
            _loggerFactory = LoggerFactory.Create(_ => _.SetMinimumLevel(LogLevel.Debug).AddDebug());
            _smtpSenderMock.Setup(_ => _.SendAsync(It.IsAny<MimeMessage>(), It.IsAny<CancellationToken>(), It.IsAny<ITransferProgress>())).Verifiable();
            _smtpSenderMock.Setup(_ => _.TrySendAsync(It.IsAny<MimeMessage>(), It.IsAny<CancellationToken>(), It.IsAny<ITransferProgress>()))
                .ReturnsAsync(true).Verifiable();
            _testEmail = new EmailWriter(_smtpSenderMock.Object, _loggerFactory.CreateLogger<EmailWriter>(), _fileSystem)
                .From("My Name", "me@localhost")
                .To("Your Name", "you@localhost")
                .Subject("Hello World")
                .BodyText("\r\ntext/plain\r\n")
                .BodyHtml("<p>text/html</p><br/>");
        }

        [Fact]
        public async Task SendAsync_VerifySentAsync()
        {
            // Act
            await _testEmail.SendAsync(It.IsAny<CancellationToken>(), It.IsAny<ITransferProgress>());
            // Assert
            _smtpSenderMock.Verify(_ => _.SendAsync(It.IsAny<MimeMessage>(), It.IsAny<CancellationToken>(), It.IsAny<ITransferProgress>()), Times.Once);
        }

        [Fact]
        public void Send_VerifySent()
        {
            // Act
            _testEmail.Send(It.IsAny<CancellationToken>());
            // Assert
            _smtpSenderMock.Verify(_ => _.SendAsync(It.IsAny<MimeMessage>(), It.IsAny<CancellationToken>(), It.IsAny<ITransferProgress>()), Times.Once);
        }

        [Fact]
        public async Task TrySendAsync_VerifySentAsync()
        {
            var isSent = await _testEmail.TrySendAsync(It.IsAny<CancellationToken>(), It.IsAny<ITransferProgress>());
            Assert.True(isSent);
            _smtpSenderMock.Verify(_ => _.TrySendAsync(It.IsAny<MimeMessage>(), It.IsAny<CancellationToken>(), It.IsAny<ITransferProgress>()), Times.Once);
        }

        [Fact]
        public void TrySend_VerifySent()
        {
            var isSent = _testEmail.TrySend(It.IsAny<CancellationToken>());
            Assert.True(isSent);
            _smtpSenderMock.Verify(_ => _.TrySendAsync(It.IsAny<MimeMessage>(), It.IsAny<CancellationToken>(), It.IsAny<ITransferProgress>()), Times.Once);
        }

        [Fact]
        public void EmailWriter_WithAttachments_VerifyBuild()
        {
            using var stream1 = _fileSystem.File.OpenRead(_attachment1Path);
            string fileName1 = _fileSystem.Path.GetFileName(_attachment1Path);
            var attachment1 = EmailWriter.GetMimePart(stream1, fileName1);
            var attachment2 = EmailWriter.GetMimePart(_attachment2Path, _fileSystem);
            var attachemnts = new MimeEntity[] { attachment1, attachment2 };
            var mimeMessage = _testEmail.Copy()
                .From("from@example.com")
                .To("to@example.com")
                .Cc("Carbon copy", "cc1@localhost")
                .Cc("cc2@localhost")
                .Bcc("Blind carbon copy", "bcc1@localhost")
                .Bcc("bcc2@localhost")
                .Subject("Hey")
                .Subject("Re: ", " friend")
                .BodyText("Hello World")
                .BodyHtml("<b>Hello World!</b>")
                .Attach(stream1, fileName1)
                .Attach(attachment2)
                .Attach(attachemnts)
                .Attach(_attachment1Path, _attachment2Path)
                .TryAttach(_attachment1Path, _attachment2Path)
                .Header("X-CampaignId", "1234")
                .Priority(MessagePriority.NonUrgent)
                .MimeMessage;
            Assert.Equal(MessagePriority.NonUrgent, mimeMessage.Priority);
            Assert.Contains(fileName1, _testEmail.ToString());
        }
    }
}