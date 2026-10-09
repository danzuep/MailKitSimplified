using MailKitSimplified.Generic.Abstractions;
using MailKitSimplified.Generic.Models;
using MailKitSimplified.Generic.Services;
using Xunit;
using GenericEmail = MailKitSimplified.Generic.Models.GenericEmail;

namespace MailKitSimplified.Generic.Tests
{
    public class GenericEmailBuilderUnitTests
    {
        [Fact]
        public void Copy_Changes_DoNotMutateOriginal()
        {
            var original = new GenericEmailBuilder()
                .DefaultFrom("sender@example.com")
                .To("recipient@example.com")
                .Subject("Original")
                .BodyText("Text")
                .BodyHtml("<p>Text</p>")
                .Header("X-Test", "original")
                .Attach("original.txt");

            var copy = original.Copy()
                .To("another@example.com")
                .Subject("Changed")
                .BodyText("Changed")
                .BodyHtml("<p>Changed</p>")
                .Attach("new.txt");
            copy.AsEmail.Headers["X-Test"] = "changed";

            Assert.Single(original.AsEmail.To);
            Assert.Equal("Original", original.AsEmail.Subject);
            Assert.Equal("Text", original.AsEmail.BodyText);
            Assert.Equal("<p>Text</p>", original.AsEmail.BodyHtml);
            Assert.Equal("original", original.AsEmail.Headers["X-Test"]);
            Assert.Single(original.AsEmail.Attachments);
        }

        [Theory]
        [InlineData("From")]
        [InlineData("ReplyTo")]
        [InlineData("To")]
        [InlineData("Cc")]
        [InlineData("Bcc")]
        public void Build_RecipientCollectionsAndContacts_AreIndependent(string collectionName)
        {
            var builder = new GenericEmailBuilder();
            var recipients = GetRecipients(builder.AsEmail, collectionName);
            var contact = GenericEmailContact.Create("original@example.com", "Original");
            contact.Name = null;
            recipients.Add(contact);

            var snapshot = builder.Build();
            var copiedRecipients = GetRecipients(snapshot, collectionName);
            contact.Name = "Changed";
            contact.EmailAddress = "changed@example.com";
            recipients.Clear();

            var copiedContact = Assert.Single(copiedRecipients);
            Assert.Null(copiedContact.Name);
            Assert.Equal("original@example.com", copiedContact.EmailAddress);
            Assert.NotSame(contact, copiedContact);
        }

        [Fact]
        public void Build_CollectionsAndByteAttachments_AreIndependent()
        {
            var bytes = new byte[] { 1, 2, 3 };
            var builder = new GenericEmailBuilder()
                .Header("X-Test", "original")
                .Attach("bytes.bin", bytes)
                .Subject("Original");

            var first = builder.Build();
            var second = builder.Build();
            bytes[0] = 9;
            builder.AsEmail.Headers.Clear();
            builder.AsEmail.Attachments.Clear();
            builder.Subject("Changed");

            Assert.Equal("original", first.Headers["X-Test"]);
            Assert.Equal("Original", first.Subject);
            var firstBytes = Assert.IsType<byte[]>(first.Attachments["bytes.bin"]);
            var secondBytes = Assert.IsType<byte[]>(second.Attachments["bytes.bin"]);
            Assert.Equal(new byte[] { 1, 2, 3 }, firstBytes);
            firstBytes[0] = 8;
            Assert.Equal(new byte[] { 1, 2, 3 }, secondBytes);
        }

        [Fact]
        public void Build_LegacyStreamsAndMissingPaths_DoNotPerformIO()
        {
            using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
            var builder = new GenericEmailBuilder()
                .Attach("stream.bin", stream)
                .Attach("missing-directory/missing-file.txt");

            var snapshot = builder.Build();

            Assert.Same(stream, snapshot.Attachments["stream.bin"]);
            Assert.Null(snapshot.Attachments["missing-directory/missing-file.txt"]);
            Assert.Equal(0L, stream.Position);
            Assert.True(stream.CanRead);
        }

        [Fact]
        public void Copy_ContactsAndByteAttachments_AreIndependent()
        {
            var original = new GenericEmailBuilder()
                .DefaultFrom("sender@example.com", "Sender")
                .To("recipient@example.com", "Recipient")
                .Attach("bytes.bin", new byte[] { 1 });

            var copy = original.Copy();
            copy.AsEmail.From[0].Name = "Changed sender";
            copy.AsEmail.To[0].EmailAddress = "changed@example.com";
            Assert.IsType<byte[]>(copy.AsEmail.Attachments["bytes.bin"])[0] = 9;

            Assert.Equal("Sender", original.AsEmail.From[0].Name);
            Assert.Equal("recipient@example.com", original.AsEmail.To[0].EmailAddress);
            Assert.Equal(new byte[] { 1 }, Assert.IsType<byte[]>(original.AsEmail.Attachments["bytes.bin"]));
        }

        [Fact]
        public void ModelFacade_AttachmentsShareCanonicalStorage()
        {
            var model = new Models.GenericEmail();
            GenericEmail canonical = model;
            IGenericEmail contract = model;
            var attachments = new Dictionary<string, object> { { "first.txt", new byte[] { 1 } } };

            model.Attachments = attachments;
            Assert.Same(attachments, canonical.Attachments);
            Assert.Same(attachments, contract.Attachments);
            canonical.Attachments = new Dictionary<string, object> { { "second.txt", null! } };

            Assert.Same(canonical.Attachments, model.Attachments);
            Assert.Single(model.AttachmentFileNames);
        }

        [Fact]
        public void Compose_ReusableDefaultsAndPerMessageFields_BuildIndependentEmails()
        {
            Action<GenericEmailBuilder> defaults = email => email
                .From("sender@example.com")
                .Header("X-Campaign", "welcome")
                .Body("Welcome", "<p>Welcome</p>");

            var template = new GenericEmailBuilder().Compose(defaults);
            var first = template.Copy().To("first@example.com").Subject("First").Build();
            var second = template.Copy().To("second@example.com").Subject("Second").Build();

            Assert.Equal("first@example.com", Assert.Single(first.To).EmailAddress);
            Assert.Equal("second@example.com", Assert.Single(second.To).EmailAddress);
            Assert.Empty(template.AsEmail.To);
            Assert.Equal("welcome", first.Headers["X-Campaign"]);
            Assert.Equal("Welcome", first.BodyText);
            Assert.Equal("<p>Welcome</p>", first.BodyHtml);
            Assert.IsType<MultipartContainerNode>(first.Body);
        }

        [Fact]
        public void Body_ComposedPartsAndTypedAttachments_ArePreservedWithoutIO()
        {
            var calls = 0;
            var attachment = new EmailAttachment("note.txt", cancellationToken =>
            {
                calls++;
                return Task.FromResult<Stream>(new MemoryStream());
            }, "text/plain");
            var body = new MultipartContainerNode(MultipartKind.Alternative, new BodyNode[]
            {
                new TextBodyNode("Original"), new HtmlBodyNode("<p>Original</p>")
            });
            var builder = new GenericEmailBuilder().Body(body).Attach(attachment);

            var snapshot = builder.Build();
            builder.BodyText("Changed");

            Assert.Same(body, snapshot.Body);
            Assert.Equal("Original", snapshot.BodyText);
            Assert.Equal("Changed", builder.AsEmail.BodyText);
            Assert.Equal("<p>Original</p>", builder.AsEmail.BodyHtml);
            Assert.Same(attachment, snapshot.Attachments["note.txt"]);
            Assert.Equal(0, calls);
        }

        [Fact]
        public void BodyLegacySetters_PreserveAlternativesAndInlineResources()
        {
            var image = new AttachmentNode(EmailAttachment.FromBytes("image.png", new byte[] { 1 }, "image/png", "image-id", true));
            var related = new MultipartContainerNode(MultipartKind.Related, new BodyNode[]
            {
                new HtmlBodyNode("<img src='cid:image-id'>"), image
            });
            var email = new GenericEmailBuilder().Body(related).BodyText("Plain").BodyHtml("<p>Updated</p>").Build();

            var alternative = Assert.IsType<MultipartContainerNode>(email.Body);
            var updatedRelated = Assert.IsType<MultipartContainerNode>(alternative.Children[1]);
            Assert.Equal(MultipartKind.Alternative, alternative.Kind);
            Assert.Equal("Plain", email.BodyText);
            Assert.Equal("<p>Updated</p>", email.BodyHtml);
            Assert.Same(image, updatedRelated.Children[1]);
        }

        private static IList<IGenericEmailContact> GetRecipients(GenericEmail email, string collectionName) =>
            collectionName switch
            {
                "From" => email.From,
                "ReplyTo" => email.ReplyTo,
                "To" => email.To,
                "Cc" => email.Cc,
                "Bcc" => email.Bcc,
                _ => throw new ArgumentOutOfRangeException(nameof(collectionName))
            };
    }
}