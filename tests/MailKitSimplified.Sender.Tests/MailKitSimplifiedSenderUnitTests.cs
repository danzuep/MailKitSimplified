global using Xunit;
global using Moq;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using MimeKit.Utils;
using MailKitSimplified.Core.Abstractions;
using MailKitSimplified.Core.Models;
using MailKitSimplified.Core.Services;
using MailKitSimplified.Sender.Abstractions;
using MailKitSimplified.Sender.Services;

namespace MailKitSimplified.Sender.Tests
{
    public class MailKitSimplifiedSenderUnitTests
    {
        private static readonly Task _completedTask = Task.CompletedTask;
        private readonly ILoggerFactory _loggerFactory;
        private readonly IEmailSender _emailSender;

        public MailKitSimplifiedSenderUnitTests()
        {
            var _fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { @"C:\Temp\attachment1.txt", new MockFileData("ABC") },
                { @"C:\Temp\attachment2.pdf", new MockFileData("123") }
            });
            _loggerFactory = LoggerFactory.Create(_ => _.SetMinimumLevel(LogLevel.Trace).AddDebug().AddConsole());
            var fileHandler = new FileHandler(_loggerFactory.CreateLogger<FileHandler>(), _fileSystem);
            var mimeAttachmentHandler = new MimeAttachmentHandler(_loggerFactory.CreateLogger<MimeAttachmentHandler>(), fileHandler);
            var senderOptions = new EmailSenderOptions("localhost");
            var options = Options.Create(senderOptions);
            _emailSender = new MimeMessageSender(options, mimeAttachmentHandler, _loggerFactory);
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
                .From("me@example.com", "My Name")
                .To("you@example.com", "Your Name")
                .To("friend@example.com")
                .Subject("Hey You")
                .Body("Hello World")
                .Attach("C:/Temp/attachment1.txt", "C:/Temp/attachment2.pdf");
            Assert.NotNull(email);
        }

        private static async Task<Stream> GetTestStream(int capacity = 1)
        {
            var randomBytes = new byte[capacity];
            new Random().NextBytes(randomBytes);
            var streamStub = new MemoryStream(capacity);
            await streamStub.WriteAsync(randomBytes);
            streamStub.Position = 0;
            return streamStub;
        }

        [Theory]
        [InlineData("attachment1.txt|attachment2.pdf")]
        [InlineData("./attachment1.txt", "./attachment2.pdf")]
        public async Task LoadFilePathsAsync_WithAnyAttachmentName_VerifyAttached(params string[] filePaths)
        {
            // Arrange
            var taskStub = GetTestStream();
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

#if DEBUG
       [Theory]
       [InlineData("attachment1.txt|attachment2.pdf")]
       [InlineData("./attachment1.txt", "./attachment2.pdf")]
       public async Task ConvertToMimeMessageAsync_WithAnyAttachmentName_VerifyAttached(params string[] filePaths)
       {
           // Arrange
           var multipart = new Multipart();
           var stream = await GetTestStream();
           foreach (var file in filePaths)
               multipart.Add(new MimePart(System.Net.Mime.MediaTypeNames.Application.Octet) {
                   FileName = Path.GetFileName(file),
                   Content = new MimeContent(stream),
                   ContentTransferEncoding = ContentEncoding.Base64,
                   ContentDisposition = new MimeKit.ContentDisposition(
                       MimeKit.ContentDisposition.Attachment),
                   ContentId = MimeUtils.GenerateMessageId()
               });
           var taskStub = Task.FromResult(new MimeMessage { Body = multipart });
           var attachmentHandler = Mock.Of<IMimeAttachmentHandler>(file => file.AddAttachments(It.IsAny<MimeMessage>(), It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()) == taskStub);
           var email = Email.Create(MimeMessageSender.Create("smtp.host")).From("me@mine").To("you@yours").Attach(filePaths).GetEmail;
           // Act
           var mimeMessage = await MimeMessageSender.ConvertToMimeMessageAsync(email, attachmentHandler, CancellationToken.None);
           // Assert
           Assert.NotNull(mimeMessage);
           Assert.True(mimeMessage.Attachments.Any());
       }
#endif

        [Fact]
        public async void TrySendAsync_WithEmail_VerifySent()
        {
            // Arrange
            var emailSenderMock = new Mock<IEmailSender>();
            emailSenderMock
                .Setup(sender => sender.SendAsync(It.IsAny<ISendableEmail>(), It.IsAny<CancellationToken>()))
                .Returns(_completedTask);
            var email = new Email(emailSenderMock.Object)
            {
                From = EmailContact.ParseEmailContacts("from@").ToList(),
                To = EmailContact.ParseEmailContacts("to@").ToList(),
                Subject = string.Empty,
                Body = string.Empty,
            };
            // Act
            var result = await email.TrySendAsync(It.IsAny<CancellationToken>());
            // Assert
            Assert.True(result);
            emailSenderMock.Verify(sender => sender.SendAsync(It.IsAny<ISendableEmail>(), It.IsAny<CancellationToken>()), Times.Once());
        }

        [Fact]
        public void SendAsync_WithEmailWriter_VerifySent()
        {
            // Arrange
            var emailMock = new Mock<ISendableEmail>();
            emailMock
                .Setup(sender => sender.SendAsync(It.IsAny<CancellationToken>()))
                .Returns(_completedTask);
            var email = EmailWriter.CreateWith(emailMock.Object);
            // Act
            var result = email.SendAsync(It.IsAny<CancellationToken>());
            // Assert
            Assert.Equal(_completedTask, result);
            emailMock.Verify(sender => sender.SendAsync(It.IsAny<CancellationToken>()), Times.Once());
        }

        //[Fact]
        //public async Task SendAsync_WithMimeMessage_VerifySentAsync()
        //{
        //    // Arrange
        //    var emailMock = new Mock<IEmail>();
        //    emailMock
        //        .Setup(sender => sender.SendAsync(It.IsAny<CancellationToken>()))
        //        .Returns(_completedTask);
        //    var emailSenderMock = new Mock<IMimeMessageSender>();
        //    emailSenderMock
        //        .Setup(sender => sender.SendAsync(It.IsAny<MimeMessage>(), It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
        //        .Verifiable();
        //    // Act
        //    await emailSenderMock.Object.SendAsync(new MimeMessage(), It.IsAny<CancellationToken>());
        //    // Assert
        //    emailSenderMock.Verify(sender => sender.SendAsync(It.IsAny<MimeMessage>(), It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()), Times.Once());
        //}

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