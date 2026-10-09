using System;
using System.Collections.Generic;
using System.Linq;
using MailKitSimplified.Generic.Abstractions;

namespace MailKitSimplified.Generic.Models
{
    /// <summary>Identifies MIME-neutral multipart composition semantics.</summary>
    public enum MultipartKind
    {
        /// <summary>Independent body parts and attachments.</summary>
        Mixed,
        /// <summary>Alternative representations ordered from least to most preferred.</summary>
        Alternative,
        /// <summary>A root body followed by its related resources.</summary>
        Related
    }

    /// <summary>An immutable header; ordered lists may contain repeated names.</summary>
    public sealed class HeaderNode : IEmailAstNode
    {
        /// <summary>Gets the printable ASCII field name.</summary>
        public string Name { get; }
        /// <summary>Gets the unfolded header value.</summary>
        public string Value { get; }

        /// <summary>Creates a header without permitting header injection.</summary>
        /// <param name="name">The field name without a colon.</param>
        /// <param name="value">The unfolded field value.</param>
        public HeaderNode(string name, string value)
        {
            if (string.IsNullOrEmpty(name) || name.Any(character => character < 33 || character > 126 || character == ':'))
                throw new ArgumentException("A printable ASCII header name without a colon is required.", nameof(name));
            if (value == null)
                throw new ArgumentNullException(nameof(value));
            if (value.IndexOfAny(new[] { '\r', '\n', '\0' }) >= 0)
                throw new ArgumentException("Header values must not contain header delimiters.", nameof(value));
            Name = name;
            Value = value;
        }

        /// <inheritdoc/>
        public void Accept(IEmailVisitor visitor) => (visitor ?? throw new ArgumentNullException(nameof(visitor))).Visit(this);
    }

    /// <summary>Base type for immutable body content and multipart children.</summary>
    public abstract class BodyNode : IEmailAstNode
    {
        /// <inheritdoc/>
        public abstract void Accept(IEmailVisitor visitor);
    }

    /// <summary>Plain-text body content.</summary>
    public sealed class TextBodyNode : BodyNode
    {
        /// <summary>Gets the plain-text content.</summary>
        public string Text { get; }

        /// <summary>Creates plain-text content.</summary>
        /// <param name="text">The content, including an optional empty body.</param>
        public TextBodyNode(string text) => Text = text ?? throw new ArgumentNullException(nameof(text));

        /// <summary>Supports C# positional pattern matching.</summary>
        /// <param name="text">The plain-text content.</param>
        public void Deconstruct(out string text) => text = Text;

        /// <inheritdoc/>
        public override void Accept(IEmailVisitor visitor) => (visitor ?? throw new ArgumentNullException(nameof(visitor))).Visit(this);
    }

    /// <summary>HTML body content; parsing and sanitization are separate operations.</summary>
    public sealed class HtmlBodyNode : BodyNode
    {
        /// <summary>Gets the HTML content.</summary>
        public string Html { get; }

        /// <summary>Creates HTML content without claiming to sanitize it.</summary>
        /// <param name="html">The HTML content.</param>
        public HtmlBodyNode(string html) => Html = html ?? throw new ArgumentNullException(nameof(html));

        /// <summary>Supports C# positional pattern matching.</summary>
        /// <param name="html">The HTML content.</param>
        public void Deconstruct(out string html) => html = Html;

        /// <inheritdoc/>
        public override void Accept(IEmailVisitor visitor) => (visitor ?? throw new ArgumentNullException(nameof(visitor))).Visit(this);
    }

    /// <summary>Deferred attachment content inside the body tree.</summary>
    public sealed class AttachmentNode : BodyNode
    {
        /// <summary>Gets the immutable attachment descriptor.</summary>
        public EmailAttachment Attachment { get; }

        /// <summary>Creates an attachment node without opening its content.</summary>
        /// <param name="attachment">The deferred attachment.</param>
        public AttachmentNode(EmailAttachment attachment) =>
            Attachment = attachment ?? throw new ArgumentNullException(nameof(attachment));

        /// <inheritdoc/>
        public override void Accept(IEmailVisitor visitor) => (visitor ?? throw new ArgumentNullException(nameof(visitor))).Visit(this);
    }

    /// <summary>A multipart container with an immutable ordered child snapshot.</summary>
    public sealed class MultipartContainerNode : BodyNode
    {
        /// <summary>Gets the composition semantics.</summary>
        public MultipartKind Kind { get; }
        /// <summary>Gets children in MIME significance order.</summary>
        public IReadOnlyList<BodyNode> Children { get; }

        /// <summary>Creates a container by copying the supplied child sequence.</summary>
        /// <param name="kind">The multipart semantics.</param>
        /// <param name="children">A nonempty sequence of nonnull body nodes.</param>
        public MultipartContainerNode(MultipartKind kind, IEnumerable<BodyNode> children)
        {
            if (!Enum.IsDefined(typeof(MultipartKind), kind))
                throw new ArgumentOutOfRangeException(nameof(kind));
            if (children == null)
                throw new ArgumentNullException(nameof(children));
            var snapshot = children.ToArray();
            if (snapshot.Length == 0 || snapshot.Any(child => child == null))
                throw new ArgumentException("Multipart content requires nonnull children.", nameof(children));
            Kind = kind;
            Children = Array.AsReadOnly(snapshot);
        }

        /// <inheritdoc/>
        public override void Accept(IEmailVisitor visitor) => (visitor ?? throw new ArgumentNullException(nameof(visitor))).Visit(this);
    }
}