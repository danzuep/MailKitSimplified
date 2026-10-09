using MailKitSimplified.Generic.Models;

namespace MailKitSimplified.Generic.Abstractions
{
    /// <summary>Dispatches email nodes without exposing provider types.</summary>
    public interface IEmailVisitor
    {
        /// <summary>Visits an ordered header.</summary>
        /// <param name="node">The header.</param>
        void Visit(HeaderNode node);

        /// <summary>Visits plain-text content.</summary>
        /// <param name="node">The text body.</param>
        void Visit(TextBodyNode node);

        /// <summary>Visits HTML content, which is not an HTML syntax tree.</summary>
        /// <param name="node">The HTML body.</param>
        void Visit(HtmlBodyNode node);

        /// <summary>Visits deferred attachment content without opening it.</summary>
        /// <param name="node">The attachment.</param>
        void Visit(AttachmentNode node);

        /// <summary>Visits a multipart container; the visitor controls child traversal.</summary>
        /// <param name="node">The multipart container.</param>
        void Visit(MultipartContainerNode node);
    }
}