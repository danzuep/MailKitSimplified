using MailKitSimplified.Generic.Abstractions;
using MailKitSimplified.Generic.Models;

namespace MailKitSimplified.Generic.Services
{
    /// <summary>Base visitor walking multipart children in their declared order.</summary>
    /// <remarks>Override multipart visits and call the base method to retain traversal.</remarks>
    public abstract class EmailAstWalker : IEmailVisitor
    {
        /// <inheritdoc/>
        public virtual void Visit(HeaderNode node) { }
        /// <inheritdoc/>
        public virtual void Visit(TextBodyNode node) { }
        /// <inheritdoc/>
        public virtual void Visit(HtmlBodyNode node) { }
        /// <inheritdoc/>
        public virtual void Visit(AttachmentNode node) { }

        /// <inheritdoc/>
        public virtual void Visit(MultipartContainerNode node)
        {
            foreach (var child in node.Children)
                child.Accept(this);
        }
    }
}