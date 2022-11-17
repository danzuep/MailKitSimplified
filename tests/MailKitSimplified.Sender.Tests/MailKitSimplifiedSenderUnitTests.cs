global using Xunit;
global using Moq;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using MailKit;
using MailKit.Net.Smtp;
using MailKitSimplified.Sender.Abstractions;
using MailKitSimplified.Sender.Services;
using MailKitSimplified.Sender.Models;
using MailKitSimplified.Sender.Extensions;

namespace MailKitSimplified.Sender.Tests
{
    public class MailKitSimplifiedSenderUnitTests
    {
        private static readonly string _logFilePath = @"C:\Temp\EmailClientSmtp.log";
        private const string _attachment1Path = @"C:\Temp\attachment1.txt";
        private const string _attachment2Path = @"C:\Temp\attachment2.pdf";
        private static readonly Task _completedTask = Task.CompletedTask;

        private readonly IEmailWriter _testEmail;

        private readonly IFileSystem _fileSystem;
        private readonly ISmtpSender _emailSender;
        private readonly IEmailWriter _emailWriter;
        private readonly ILoggerFactory _loggerFactory;

        public MailKitSimplifiedSenderUnitTests()
        {
            _fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { _logFilePath, new MockFileData(string.Empty) },
                { _attachment1Path, new MockFileData("ABC") },
                { _attachment2Path, new MockFileData("123") }
            });
            _loggerFactory = LoggerFactory.Create(_ => _.SetMinimumLevel(LogLevel.Trace).AddDebug().AddConsole());
            var protocolLoggerMock = new Mock<IProtocolLogger>();
            //var mailKitProtocolLogger = new MailKitProtocolLogger(null, _fileSystem, _loggerFactory.CreateLogger<MailKitProtocolLogger>());
            var smtpClientMock = new Mock<ISmtpClient>();
            smtpClientMock.Setup(_ => _.SendAsync(It.IsAny<MimeMessage>(), It.IsAny<CancellationToken>(), It.IsAny<ITransferProgress>()))
                .ReturnsAsync("Mail accepted").Verifiable();
            var senderOptions = new EmailSenderOptions("localhost");
            var options = Options.Create(senderOptions);
            _emailSender = new SmtpSender(options, _loggerFactory.CreateLogger<SmtpSender>(), protocolLoggerMock.Object, smtpClientMock.Object);
            _emailWriter = new EmailWriter(_emailSender, _loggerFactory.CreateLogger<EmailWriter>(), _fileSystem);
            _testEmail = _emailWriter
                .From("My Name", "me@localhost")
                .To("Your Name", "you@localhost")
                .Subject("Hello World")
                .BodyText("\r\ntext/plain\r\n")
                .BodyHtml("<p>text/html</p><br/>");
        }

        [Theory]
        [InlineData("localhost")]
        [InlineData("smtp.google.com")]
        [InlineData("smtp.sendgrid.com")]
        [InlineData("smtp.mail.yahoo.com")]
        [InlineData("outlook.office365.com")]
        [InlineData("smtp.freesmtpservers.com")]
        public void WriteEmail_WithSmtpSender_VerifyCreatedAsync(string smtpHost)
        {
            using var smtpSender = SmtpSender.Create(smtpHost);
            var email = smtpSender.WriteEmail
                .From("My Name", "me@example.com")
                .To("Your Name", "you@example.com")
                .Subject("Hello World")
                .BodyHtml("We did it!");
            Assert.NotNull(email?.MimeMessage);
            Assert.True(email?.ToString()?.Length > 0);
        }

        [Fact]
        public async Task TrySendAsync_WithAttachment_VerifySentAsync()
        {
            var isSent = await _testEmail
                .TryAttach(_attachment1Path, _attachment2Path)
                .TrySendAsync();
            Assert.True(isSent);
        }

        [Fact]
        public async Task SendAsync_WithAttachments_ReturnsCompletedTask()
        {
            using var stream1 = _fileSystem.File.OpenRead(_attachment1Path);
            string fileName1 = _fileSystem.Path.GetFileName(_attachment1Path);
            string fileName2 = _fileSystem.Path.GetFileName(_attachment2Path);
            var attachment1 = EmailWriter.GetMimePart(_attachment1Path, _fileSystem);
            var attachment2 = EmailWriter.GetMimePart(_attachment2Path, _fileSystem);
            var attachemnts = new MimeEntity[] { attachment1, attachment2 };
            await _testEmail
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
                .Attach(attachemnts)
                .Attach(_attachment1Path, _attachment2Path)
                .Header("X-CampaignId", "1234")
                .SendAsync();
            var attachmentNames = attachemnts.GetAttachmentNames();
            Assert.Contains(fileName1, attachmentNames);
            Assert.Contains(fileName2, attachmentNames);
        }

        [Fact]
        public void TrySend_VerifySent()
        {
            var isSent = _testEmail.TrySend();
            _testEmail.Send();
            Assert.True(isSent);
        }

        [Fact]
        public void SendAsync_WithEmailWriter_VerifySent()
        {
            // Arrange
            var emailSenderMock = new Mock<ISmtpSender>();
            emailSenderMock
                .Setup(sender => sender.SendAsync(It.IsAny<MimeMessage>(), It.IsAny<CancellationToken>(), It.IsAny<ITransferProgress>()))
                .Returns(_completedTask);
            var email = new EmailWriter(emailSenderMock.Object)
                .From("from@localhost").To("to@localhost");
            // Act
            var result = email.SendAsync(It.IsAny<CancellationToken>(), It.IsAny<ITransferProgress>());
            // Assert
            Assert.Equal(_completedTask, result);
            emailSenderMock.Verify(sender => sender.SendAsync(It.IsAny<MimeMessage>(), It.IsAny<CancellationToken>(), It.IsAny<ITransferProgress>()), Times.Once());
        }

        [Fact]
        public async void TrySendAsync_WithEmailWriter_VerifySent()
        {
            // Arrange
            var emailSenderMock = new Mock<ISmtpSender>();
            emailSenderMock
                .Setup(sender => sender.TrySendAsync(It.IsAny<MimeMessage>(), It.IsAny<CancellationToken>(), It.IsAny<ITransferProgress>()))
                .Returns(Task.FromResult(true));
            var email = new EmailWriter(emailSenderMock.Object)
                .From("MyName@localhost").To("your.name@localhost");
            // Act
            var result = await email.TrySendAsync(It.IsAny<CancellationToken>(), It.IsAny<ITransferProgress>());
            // Assert
            Assert.True(result);
            emailSenderMock.Verify(sender => sender.TrySendAsync(It.IsAny<MimeMessage>(), It.IsAny<CancellationToken>(), It.IsAny<ITransferProgress>()), Times.Once());
        }
    }
}