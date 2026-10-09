using MailKitSimplified.Generic.Abstractions;
using MailKitSimplified.Generic.Models;
using MailKitSimplified.Generic.Services;
using Xunit;
using GenericEmail = MailKitSimplified.Generic.Services.GenericEmail;

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