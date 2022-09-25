global using Xunit;
global using Moq;
using System.Threading;
using System.Threading.Tasks;
using MimeKit;
using MailKitSimplified.Sender.Abstractions;

namespace MailKitSimplified.Sender.Tests
{
    public class MailKitSimpleSenderTests
    {
        private static readonly Task _completedTask = Task.CompletedTask;

        [Fact]
        public void CreateEmail_WithIMimeEmailSender_VerifyCreated()
        {
            var emailSenderMock = Mock.Of<IMimeEmailSender>(sender =>
                sender.Email == Mock.Of<IEmail>());
            Assert.NotNull(emailSenderMock.Email);
        }

        [Theory]
        [InlineData("smtp.example.com")]
        public void CreateEmail_WithEmailSender_VerifyCreated(string smtpHost)
        {
            IMimeEmailSender emailSender = EmailSender.Create(smtpHost);
            var email = emailSender.Email
                .From("test")
                .To("test")
                .Subject("Hi")
                .Body("~");
            Assert.NotNull(email);
        }

        [Fact]
        public void SendAsync_WithMimeEmail_VerifySent()
        {
            var emailSenderMock = new Mock<IMimeEmailSender>();
            var email = new MimeEmail(emailSenderMock.Object);
            emailSenderMock
                .Setup(sender => sender.SendAsync(It.IsAny<MimeMessage>(), It.IsAny<CancellationToken>()))
                .Returns(_completedTask);
            var result = email.SendAsync(It.IsAny<CancellationToken>());
            Assert.Equal(_completedTask, result);
            emailSenderMock.Verify(sender => sender.SendAsync(It.IsAny<MimeMessage>(), It.IsAny<CancellationToken>()), Times.Once());
        }
    }
}