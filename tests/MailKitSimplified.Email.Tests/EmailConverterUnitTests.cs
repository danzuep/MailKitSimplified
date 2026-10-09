using MailKitSimplified.Email.Extensions;
using MailKitSimplified.Generic.Models;
using MailKitSimplified.Generic.Services;
using MimeKit;
using Xunit;

namespace MailKitSimplified.Email.Tests
{
    public class EmailConverterUnitTests
    {
        [Fact]
        public async Task ToMimeMessageAsync_MapsFluentAlternativesHeadersAndAttachments()
        {
            var email = new GenericEmailBuilder()
                .From("sender@example.com")
                .To("recipient@example.com")
                .Subject("Hello")
                .Header("X-Test", "value")
                .Body("Plain", "<p>HTML</p>")
                .Attach(EmailAttachment.FromBytes("note.txt", new byte[] { 1, 2 }, "text/plain"))
                .Build();

            using var message = await email.ToMimeMessageAsync(TestContext.Current.CancellationToken);

            Assert.Equal("Hello", message.Subject);
            Assert.Equal("value", message.Headers["X-Test"]);
            Assert.Equal("Plain", message.TextBody);
            Assert.Equal("<p>HTML</p>", message.HtmlBody);
            var mixed = Assert.IsType<Multipart>(message.Body);
            Assert.Equal("mixed", mixed.ContentType.MediaSubtype);
            Assert.Equal("alternative", Assert.IsType<MultipartAlternative>(mixed[0]).ContentType.MediaSubtype);
            var attachment = Assert.IsType<MimePart>(Assert.Single(message.Attachments));
            Assert.Equal("note.txt", attachment.FileName);
            using var content = new MemoryStream();
            Assert.NotNull(attachment.Content);
            await attachment.Content.DecodeToAsync(content, TestContext.Current.CancellationToken);
            Assert.Equal(new byte[] { 1, 2 }, content.ToArray());
        }

        [Fact]
        public async Task ToMimeMessageAsync_PreservesRelatedInlineContent()
        {
            var body = new MultipartContainerNode(MultipartKind.Related, new BodyNode[]
            {
                new HtmlBodyNode("<img src='cid:image-id'>"),
                new AttachmentNode(EmailAttachment.FromBytes("image.png", new byte[] { 1 }, "image/png", "image-id", true))
            });

            using var message = await new GenericEmailBuilder().Body(body).Build()
                .ToMimeMessageAsync(TestContext.Current.CancellationToken);

            var related = Assert.IsType<MultipartRelated>(message.Body);
            Assert.Equal("related", related.ContentType.MediaSubtype);
            var image = Assert.IsType<MimePart>(related[1]);
            Assert.Equal("image-id", image.ContentId);
            Assert.NotNull(image.ContentDisposition);
            Assert.Equal("inline", image.ContentDisposition.Disposition);
        }

        [Fact]
        public async Task ToMimeMessageAsync_RepeatedConversionOpensFreshOwnedStreams()
        {
            var opened = new List<MemoryStream>();
            var attachment = new EmailAttachment("note.txt", cancellationToken =>
            {
                var stream = new MemoryStream(new byte[] { 1 });
                opened.Add(stream);
                return Task.FromResult<Stream>(stream);
            });
            var email = new GenericEmailBuilder().Body("Plain").Attach(attachment).Build();
            Assert.Empty(opened);

            using (await email.ToMimeMessageAsync(TestContext.Current.CancellationToken)) { }
            using (await email.ToMimeMessageAsync(TestContext.Current.CancellationToken)) { }

            Assert.Equal(2, opened.Count);
            Assert.NotSame(opened[0], opened[1]);
            Assert.All(opened, stream => Assert.False(stream.CanRead));
        }

        [Fact]
        public async Task ToMimeMessageAsync_FailureDisposesAlreadyOpenedContent()
        {
            var opened = new MemoryStream(new byte[] { 1 });
            var email = new GenericEmailBuilder().Body("Plain")
                .Attach(new EmailAttachment("first.txt", cancellationToken => Task.FromResult<Stream>(opened)))
                .Attach(new EmailAttachment("second.txt", cancellationToken => throw new IOException("Expected")))
                .Build();

            await Assert.ThrowsAsync<IOException>(() => email.ToMimeMessageAsync(TestContext.Current.CancellationToken));
            Assert.False(opened.CanRead);
        }

        [Fact]
        public async Task ToMimeMessageAsync_LegacyStreamsRemainCallerOwned()
        {
            using var stream = new MemoryStream(new byte[] { 1, 2 });
            var email = new GenericEmailBuilder().BodyText("Plain").Attach("note.bin", stream).Build();

            using (var message = await email.ToMimeMessageAsync(TestContext.Current.CancellationToken))
                Assert.Single(message.Attachments);

            Assert.True(stream.CanRead);
        }

        [Fact]
        public async Task ToMimeMessageAsync_PreCancelledDoesNotOpenAttachments()
        {
            var calls = 0;
            var email = new GenericEmailBuilder().Attach(new EmailAttachment("note.txt", cancellationToken =>
            {
                calls++;
                return Task.FromResult<Stream>(new MemoryStream());
            })).Build();
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => email.ToMimeMessageAsync(cancellation.Token));
            Assert.Equal(0, calls);
        }
    }
}