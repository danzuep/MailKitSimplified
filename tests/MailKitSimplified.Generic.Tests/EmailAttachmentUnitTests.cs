using MailKitSimplified.Generic.Models;
using Xunit;

namespace MailKitSimplified.Generic.Tests
{
    public class EmailAttachmentUnitTests
    {
        [Fact]
        public async Task FromBytes_OpensIndependentReadOnlySnapshots()
        {
            var content = new byte[] { 1, 2, 3 };
            var attachment = EmailAttachment.FromBytes("content.bin", content);
            content[0] = 9;

            using var first = await attachment.OpenReadAsync(TestContext.Current.CancellationToken);
            using var second = await attachment.OpenReadAsync(TestContext.Current.CancellationToken);

            Assert.NotSame(first, second);
            Assert.False(first.CanWrite);
            Assert.Equal(1, first.ReadByte());
            Assert.Equal(1, second.ReadByte());
            Assert.Equal(2, first.ReadByte());
            Assert.False(Assert.IsType<MemoryStream>(first).TryGetBuffer(out _));
        }

        [Fact]
        public async Task Constructor_DefersFactoryAndPassesCancellation()
        {
            var calls = 0;
            var token = TestContext.Current.CancellationToken;
            var attachment = new EmailAttachment("content.bin", cancellationToken =>
            {
                Assert.Equal(token, cancellationToken);
                calls++;
                return Task.FromResult<Stream>(new MemoryStream());
            });
            Assert.Equal(0, calls);

            using var first = await attachment.OpenReadAsync(token);
            using var second = await attachment.OpenReadAsync(token);

            Assert.Equal(2, calls);
            Assert.NotSame(first, second);
        }

        [Fact]
        public async Task FromFile_MissingFile_IsOnlyCheckedWhenOpened()
        {
            var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "missing.bin");
            var attachment = EmailAttachment.FromFile(path);

            Assert.Equal("missing.bin", attachment.FileName);
            await Assert.ThrowsAsync<DirectoryNotFoundException>(() =>
                attachment.OpenReadAsync(TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task OpenReadAsync_PreCancelled_DoesNotInvokeFactory()
        {
            var calls = 0;
            var attachment = new EmailAttachment("content.bin", cancellationToken =>
            {
                calls++;
                return Task.FromResult<Stream>(new MemoryStream());
            });
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => attachment.OpenReadAsync(cancellation.Token));
            Assert.Equal(0, calls);
        }

        [Fact]
        public async Task OpenReadAsync_CancelledAfterFactory_DisposesReturnedStream()
        {
            using var cancellation = new CancellationTokenSource();
            var stream = new MemoryStream();
            var attachment = new EmailAttachment("content.bin", cancellationToken =>
            {
                cancellation.Cancel();
                return Task.FromResult<Stream>(stream);
            });

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => attachment.OpenReadAsync(cancellation.Token));
            Assert.False(stream.CanRead);
        }

        [Fact]
        public async Task OpenReadAsync_UnreadableStream_DisposesReturnedStream()
        {
            var stream = new UnreadableStream();
            var attachment = new EmailAttachment("content.bin", cancellationToken => Task.FromResult<Stream>(stream));

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                attachment.OpenReadAsync(TestContext.Current.CancellationToken));
            Assert.True(stream.IsDisposed);
        }

        [Fact]
        public async Task OpenReadAsync_NullStream_Throws()
        {
            var attachment = new EmailAttachment("content.bin", cancellationToken => Task.FromResult<Stream>(null!));

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                attachment.OpenReadAsync(TestContext.Current.CancellationToken));
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("file\r\nBcc: victim@example.com")]
        [InlineData("file\0.bin")]
        public void Constructor_InvalidFileName_Throws(string fileName)
        {
            Assert.Throws<ArgumentException>(() => new EmailAttachment(fileName,
                cancellationToken => Task.FromResult<Stream>(new MemoryStream())));
        }

        [Theory]
        [InlineData("text/plain\r\nX-Test: injected", null)]
        [InlineData("text/plain", "id\r\nX-Test: injected")]
        public void Constructor_InvalidHeaderMetadata_Throws(string contentType, string? contentId)
        {
            Assert.Throws<ArgumentException>(() => new EmailAttachment("content.txt",
                cancellationToken => Task.FromResult<Stream>(new MemoryStream()), contentType, contentId));
        }

        [Fact]
        public void Constructor_InlineWithoutContentId_Throws()
        {
            Assert.Throws<ArgumentException>(() => new EmailAttachment("image.png",
                cancellationToken => Task.FromResult<Stream>(new MemoryStream()), "image/png", isInline: true));
        }

        private sealed class UnreadableStream : MemoryStream
        {
            public override bool CanRead => false;
            public bool IsDisposed { get; private set; }

            protected override void Dispose(bool disposing)
            {
                IsDisposed = true;
                base.Dispose(disposing);
            }
        }
    }
}