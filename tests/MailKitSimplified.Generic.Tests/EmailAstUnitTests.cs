using MailKitSimplified.Generic.Abstractions;
using MailKitSimplified.Generic.Models;
using MailKitSimplified.Generic.Services;
using Xunit;

namespace MailKitSimplified.Generic.Tests
{
    public class EmailAstUnitTests
    {
        [Fact]
        public void Accept_UsesTypedDispatchAndOrderedTraversal()
        {
            var attachment = EmailAttachment.FromBytes("content.bin", new byte[] { 1 });
            var body = new MultipartContainerNode(MultipartKind.Mixed, new BodyNode[]
            {
                new MultipartContainerNode(MultipartKind.Alternative, new BodyNode[]
                {
                    new TextBodyNode("Plain"), new HtmlBodyNode("<p>HTML</p>")
                }),
                new AttachmentNode(attachment)
            });
            var visitor = new RecordingVisitor();

            new HeaderNode("X-Test", "value").Accept(visitor);
            body.Accept(visitor);

            Assert.Equal(new[] { "header:X-Test", "Mixed", "Alternative", "text:Plain", "html:<p>HTML</p>", "attachment:content.bin" }, visitor.Nodes);
        }

        [Fact]
        public void Multipart_ChildrenAreAnImmutableSnapshot()
        {
            var children = new List<BodyNode> { new TextBodyNode("Original") };
            var multipart = new MultipartContainerNode(MultipartKind.Mixed, children);
            children[0] = new TextBodyNode("Changed");
            children.Clear();

            Assert.Equal("Original", Assert.IsType<TextBodyNode>(Assert.Single(multipart.Children)).Text);
            Assert.Throws<NotSupportedException>(() => ((IList<BodyNode>)multipart.Children).Clear());
        }

        [Theory]
        [InlineData("", "value")]
        [InlineData("X:Test", "value")]
        [InlineData("X Test", "value")]
        [InlineData("X-Test", "value\r\nBcc: victim@example.com")]
        [InlineData("X-Test", "value\0")]
        public void Header_InvalidNameOrValue_RejectsInjection(string name, string value)
        {
            Assert.Throws<ArgumentException>(() => new HeaderNode(name, value));
        }

        [Fact]
        public void Validate_AcceptsAlternativeAndRelatedWithoutOpeningContent()
        {
            var calls = 0;
            var image = new EmailAttachment("image.png", cancellationToken =>
            {
                calls++;
                return Task.FromResult<Stream>(new MemoryStream());
            }, "image/png", "image-id", true);
            var body = new MultipartContainerNode(MultipartKind.Alternative, new BodyNode[]
            {
                new TextBodyNode("Plain"),
                new MultipartContainerNode(MultipartKind.Related, new BodyNode[]
                {
                    new HtmlBodyNode("<img src='cid:image-id'>"), new AttachmentNode(image)
                })
            });

            EmailAstValidator.Validate(body);

            Assert.Equal(0, calls);
        }

        [Fact]
        public void Validate_RejectsDepthNodeAndTextLimits()
        {
            var body = new MultipartContainerNode(MultipartKind.Mixed, new BodyNode[]
            {
                new TextBodyNode("123"), new HtmlBodyNode("456")
            });

            Assert.Throws<ArgumentException>(() => EmailAstValidator.Validate(body, maxDepth: 1));
            Assert.Throws<ArgumentException>(() => EmailAstValidator.Validate(body, maxNodeCount: 2));
            Assert.Throws<ArgumentException>(() => EmailAstValidator.Validate(body, maxTextCharacters: 5));
            EmailAstValidator.Validate(body, maxDepth: 2, maxNodeCount: 3, maxTextCharacters: 6);
        }

        [Fact]
        public void Validate_RejectsAttachmentAsAlternative()
        {
            var body = new MultipartContainerNode(MultipartKind.Alternative, new BodyNode[]
            {
                new TextBodyNode("Plain"), new AttachmentNode(EmailAttachment.FromBytes("file.bin", new byte[] { 1 }))
            });

            Assert.Throws<ArgumentException>(() => EmailAstValidator.Validate(body));
        }

        [Fact]
        public void Validate_RejectsNonInlineRelatedResourceAndDuplicateIds()
        {
            var regular = new AttachmentNode(EmailAttachment.FromBytes("file.bin", new byte[] { 1 }));
            var inline = new AttachmentNode(EmailAttachment.FromBytes("image.png", new byte[] { 1 }, "image/png", "same-id", true));
            var nonInline = new MultipartContainerNode(MultipartKind.Related, new BodyNode[] { new HtmlBodyNode("HTML"), regular });
            var duplicate = new MultipartContainerNode(MultipartKind.Related, new BodyNode[] { new HtmlBodyNode("HTML"), inline, inline });

            Assert.Throws<ArgumentException>(() => EmailAstValidator.Validate(nonInline));
            Assert.Throws<ArgumentException>(() => EmailAstValidator.Validate(duplicate));
        }

        [Fact]
        public void Multipart_RejectsEmptyNullAndUnknownChildren()
        {
            Assert.Throws<ArgumentException>(() => new MultipartContainerNode(MultipartKind.Mixed, Array.Empty<BodyNode>()));
            Assert.Throws<ArgumentException>(() => new MultipartContainerNode(MultipartKind.Mixed, new BodyNode[] { null! }));
            Assert.Throws<ArgumentOutOfRangeException>(() => new MultipartContainerNode((MultipartKind)99, new BodyNode[] { new TextBodyNode("text") }));
        }

        [Fact]
        public void TextNodes_SupportCSharpPositionalPatterns()
        {
            IEmailAstNode node = new TextBodyNode("Plain");

            var text = node switch
            {
                TextBodyNode(var plain) => plain,
                HtmlBodyNode(var html) => html,
                _ => throw new InvalidOperationException()
            };

            Assert.Equal("Plain", text);
            Assert.Throws<ArgumentNullException>(() => node.Accept(null!));
        }

        [Fact]
        public void Rewrite_UnchangedTree_PreservesIdentity()
        {
            var body = new MultipartContainerNode(MultipartKind.Alternative, new BodyNode[]
            {
                new TextBodyNode("Plain"), new HtmlBodyNode("<p>HTML</p>")
            });

            Assert.Same(body, new IdentityRewriter().Rewrite(body));
        }

        [Fact]
        public void Rewrite_ChangedText_PreservesOriginalAndUnchangedNodes()
        {
            var calls = 0;
            var attachment = new AttachmentNode(new EmailAttachment("content.bin", cancellationToken =>
            {
                calls++;
                return Task.FromResult<Stream>(new MemoryStream());
            }));
            var body = new MultipartContainerNode(MultipartKind.Mixed, new BodyNode[]
            {
                new TextBodyNode("Plain"), attachment
            });

            var rewritten = Assert.IsType<MultipartContainerNode>(new UppercaseTextRewriter().Rewrite(body));

            Assert.NotSame(body, rewritten);
            Assert.Equal("Plain", Assert.IsType<TextBodyNode>(body.Children[0]).Text);
            Assert.Equal("PLAIN", Assert.IsType<TextBodyNode>(rewritten.Children[0]).Text);
            Assert.Same(attachment, rewritten.Children[1]);
            Assert.Equal(0, calls);
        }

        [Fact]
        public void Rewrite_InvalidReplacement_IsRejected()
        {
            Assert.Throws<InvalidOperationException>(() => new NullRewriter().Rewrite(new TextBodyNode("Plain")));
            var related = new MultipartContainerNode(MultipartKind.Related, new BodyNode[] { new HtmlBodyNode("HTML") });
            Assert.Throws<ArgumentException>(() => new AttachmentReplacementRewriter().Rewrite(related));
        }

        private sealed class IdentityRewriter : EmailAstRewriter { }

        private sealed class UppercaseTextRewriter : EmailAstRewriter
        {
            protected override BodyNode RewriteText(TextBodyNode node) => new TextBodyNode(node.Text.ToUpperInvariant());
        }

        private sealed class NullRewriter : EmailAstRewriter
        {
            protected override BodyNode RewriteText(TextBodyNode node) => null!;
        }

        private sealed class AttachmentReplacementRewriter : EmailAstRewriter
        {
            protected override BodyNode RewriteHtml(HtmlBodyNode node) =>
                new AttachmentNode(EmailAttachment.FromBytes("content.bin", new byte[] { 1 }));
        }

        private sealed class RecordingVisitor : EmailAstWalker
        {
            public List<string> Nodes { get; } = new();
            public override void Visit(HeaderNode node) => Nodes.Add("header:" + node.Name);
            public override void Visit(TextBodyNode node) => Nodes.Add("text:" + node.Text);
            public override void Visit(HtmlBodyNode node) => Nodes.Add("html:" + node.Html);
            public override void Visit(AttachmentNode node) => Nodes.Add("attachment:" + node.Attachment.FileName);

            public override void Visit(MultipartContainerNode node)
            {
                Nodes.Add(node.Kind.ToString());
                base.Visit(node);
            }
        }
    }
}