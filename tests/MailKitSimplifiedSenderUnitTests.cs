global using Xunit;
global using Moq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MimeKit;
using MailKitSimplified.Core.Abstractions;
using MailKitSimplified.Sender.Abstractions;
using MailKitSimplified.Sender.Services;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System;
using System.Diagnostics;
using MailKitSimplified.Core.Models;
using MailKitSimplified.Core.Services;

namespace MailKitSimplified.Sender.Tests
{
    public class MailKitSimplifiedSenderUnitTests
    {
        private static readonly Task _completedTask = Task.CompletedTask;
        private readonly ILoggerFactory _loggerFactory;

        public MailKitSimplifiedSenderUnitTests()
        {
            _loggerFactory = LoggerFactory.Create(_ => _.SetMinimumLevel(LogLevel.Trace).AddDebug().AddConsole());
        }

        [Theory]
        [InlineData("smtp.google.com")]
        [InlineData("smtp.sendgrid.com")]
        [InlineData("smtp.mail.yahoo.com")]
        [InlineData("outlook.office365.com")]
        public void WriteEmail_WithEmailSender_VerifyCreated(string smtpHost)
        {
            using var emailSender = MimeMessageSender.Create(smtpHost);
            var email = emailSender.WriteEmail
                .From("from.test@example.com")
                .To("to.test@example.com")
                .Subject("Hi")
                .Body("~")
                .Attach("./attachment1.txt");
            Assert.NotNull(email);
        }

        [Fact]
        public void WriteEmail_WithMimeEmailSender_VerifyCreated()
        {
            using var emailSender = MimeMessageSender.Create("mail.example.com");
            var email = emailSender.MimeEmail
                .From("from")
                .To("to")
                .Subject("Hi")
                .Body("~")
                .Attach("./attachment1.txt");
            Assert.NotNull(email);
        }

        [Theory]
        [InlineData("attachment1.txt|attachment2.pdf")]
        [InlineData("./attachment1.txt", "./attachment2.pdf")]
        public async Task LoadFilePathsAsync_WithAnyAttachmentName_VerifyAttached(params string[] filePaths)
        {
            // Arrange
            int capacity = 1;
            var randomBytes = new byte[capacity];
            new Random().NextBytes(randomBytes);
            using var streamStub = new MemoryStream(capacity);
            await streamStub.WriteAsync(randomBytes);
            var taskStub = Task.FromResult(streamStub as Stream);
            var fileHandler = Mock.Of<IFileHandler>(file => file.GetFileStreamAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()) == taskStub);
            IMimeAttachmentHandler attachmentHandler = new MimeAttachmentHandler(_loggerFactory?.CreateLogger<MimeAttachmentHandler>(), fileHandler);
            // Act
            var attachments = filePaths.Length == 1 ?
                await attachmentHandler.LoadFilePathAsync(filePaths[0]) :
                await attachmentHandler.LoadFilePathsAsync(filePaths);
            // Assert
            Assert.NotNull(attachments);
            Assert.True(attachments.Any());
        }

        [Fact]
        public async void TrySendAsync_WithEmail_VerifySent()
        {
            // Arrange
            var emailSenderMock = new Mock<IEmailSender>();
            emailSenderMock
                .Setup(sender => sender.SendAsync(It.IsAny<IEmail>(), It.IsAny<CancellationToken>()))
                .Returns(_completedTask);
            var email = new Email(emailSenderMock.Object)
            {
                From = new EmailContact("from@"),
                To = EmailContact.ParseEmailContacts("to@").ToList(),
                Subject = string.Empty,
                Body = string.Empty,
            };
            // Act
            var result = await email.TrySendAsync(It.IsAny<CancellationToken>());
            // Assert
            Assert.True(result);
            emailSenderMock.Verify(sender => sender.SendAsync(It.IsAny<IEmail>(), It.IsAny<CancellationToken>()), Times.Once());
        }

        [Fact]
        public void SendAsync_WithEmailWriter_VerifySent()
        {
            // Arrange
            var emailSenderMock = new Mock<IEmail>();
            emailSenderMock
                .Setup(sender => sender.SendAsync(It.IsAny<CancellationToken>()))
                .Returns(_completedTask);
            var email = EmailWriter.CreateFrom(emailSenderMock.Object);
            // Act
            var result = email.SendAsync(It.IsAny<CancellationToken>());
            // Assert
            Assert.Equal(_completedTask, result);
            emailSenderMock.Verify(sender => sender.SendAsync(It.IsAny<CancellationToken>()), Times.Once());
        }

        [Fact]
        public void SendAsync_WithMimeEmailWriter_VerifySent()
        {
            // Arrange
            var emailSenderMock = new Mock<IMimeMessageSender>();
            emailSenderMock
                .Setup(sender => sender.SendAsync(It.IsAny<MimeMessage>(), It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
                .Returns(_completedTask);
            var email = MimeMessageWriter.CreateFrom(emailSenderMock.Object);
            // Act
            var result = email.SendAsync(It.IsAny<CancellationToken>());
            // Assert
            Assert.Equal(_completedTask, result);
            emailSenderMock.Verify(sender => sender.SendAsync(It.IsAny<MimeMessage>(), It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()), Times.Once());
        }

        [Fact]
        public async Task TrySendAsync_WithInvalidSmtpHost_VerifyNotSentAsync()
        {
            using var emailSender = MimeMessageSender.Create("mail.example.com");
            var isSent = await emailSender.WriteEmail
                .From("from")
                .To("to")
                .Subject("Hi")
                .Body("~")
                .Attach("./attachment1.txt")
                .TrySendAsync();
            Assert.False(isSent);
        }

#if DEBUG
        [Theory]
        [InlineData("./MailKitSimplified.Sender.Tests.dll")]
        [InlineData("./MailKitSimplified.Sender.Tests.pdb")]
        public async Task GetFileStreamAsync_WithIFileHandler_VerifyStreamContainsData(string filePath)
        {
            IFileHandler fileHandler = new FileHandler(_loggerFactory?.CreateLogger<FileHandler>());
            var stream = await fileHandler.GetFileStreamAsync(filePath);
            Assert.NotNull(stream);
            Assert.True(stream.Length > 0);
        }

//        [Theory]
//        [InlineData("smtp.freesmtpservers.com")]
//        public async Task SendEmail_WithMimeEmailWriter_EndToEndTest(string smtpHost, int port = 25, string log = @"C:\Temp\smptLog.txt")
//        {
//            var options = Options.Create(new EmailSenderOptions(smtpHost, port, protocolLog: log));
//            using var emailSender = new EmailSender(options, _loggerFactory);
//            var email = new MimeEmailWriter(emailSender)
//                .From("mailkitsimplifiedsender@freesmtpservers.com")
//                .To("mailkitsimplifiedsender@freesmtpservers.com")
//                .Subject("Hi1")
//                .Body("~");
//            await email.SendAsync();
//            Assert.NotNull(email);
//        }

//        [Theory]
//        [InlineData("smtp.freesmtpservers.com")]
//        public async Task SendEmail_WithEmailWriter_EndToEndTest(string smtpHost, int port = 25, string log = @"C:\Temp\smptLog.txt")
//        {
//            var options = Options.Create(new EmailSenderOptions(smtpHost, port, protocolLog: log));
//            using var emailSender = new EmailSender(options, _loggerFactory);
//            var email = new EmailWriter(emailSender)
//                .From("mailkitsimplifiedsender@freesmtpservers.com")
//                .To("mailkitsimplifiedsender@freesmtpservers.com")
//                .Subject("Hi2")
//                .Body("~");
//            await email.SendAsync();
//            Assert.NotNull(email);
//        }
#endif
    }
}