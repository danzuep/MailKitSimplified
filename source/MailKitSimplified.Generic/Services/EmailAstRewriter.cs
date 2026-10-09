using System;
using System.Collections.Generic;
using MailKitSimplified.Generic.Models;

namespace MailKitSimplified.Generic.Services
{
    /// <summary>Transforms immutable body nodes using C# pattern matching.</summary>
    /// <remarks>HTML transformations must use a dedicated parser when sanitization is required.</remarks>
    public abstract class EmailAstRewriter
    {
        /// <summary>Validates and rewrites a tree without mutating or opening its content.</summary>
        /// <param name="root">The source body.</param>
        /// <returns>A validated tree, retaining unchanged node instances.</returns>
        public BodyNode Rewrite(BodyNode root)
        {
            EmailAstValidator.Validate(root);
            var result = RewriteNode(root);
            EmailAstValidator.Validate(result);
            return result;
        }

        /// <summary>Transforms plain-text content; the default preserves the node.</summary>
        /// <param name="node">The original node.</param>
        /// <returns>The original node or its immutable replacement.</returns>
        protected virtual BodyNode RewriteText(TextBodyNode node) => node;

        /// <summary>Transforms HTML content without implicitly sanitizing it.</summary>
        /// <param name="node">The original node.</param>
        /// <returns>The original node or its immutable replacement.</returns>
        protected virtual BodyNode RewriteHtml(HtmlBodyNode node) => node;

        /// <summary>Transforms attachment metadata without opening its content.</summary>
        /// <param name="node">The original node.</param>
        /// <returns>The original node or its immutable replacement.</returns>
        protected virtual BodyNode RewriteAttachment(AttachmentNode node) => node;

        /// <summary>Transforms a multipart container after its children have been rewritten.</summary>
        /// <param name="node">The container with rewritten children.</param>
        /// <returns>The original node or its immutable replacement.</returns>
        protected virtual BodyNode RewriteMultipart(MultipartContainerNode node) => node;

        private BodyNode RewriteNode(BodyNode node)
        {
            BodyNode result;
            switch (node)
            {
                case TextBodyNode text:
                    result = RewriteText(text);
                    break;
                case HtmlBodyNode html:
                    result = RewriteHtml(html);
                    break;
                case AttachmentNode attachment:
                    result = RewriteAttachment(attachment);
                    break;
                case MultipartContainerNode multipart:
                    List<BodyNode> changedChildren = null;
                    for (var index = 0; index < multipart.Children.Count; index++)
                    {
                        var original = multipart.Children[index];
                        var rewritten = RewriteNode(original);
                        if (changedChildren == null && !ReferenceEquals(original, rewritten))
                            changedChildren = new List<BodyNode>(multipart.Children);
                        if (changedChildren != null)
                            changedChildren[index] = rewritten;
                    }
                    result = RewriteMultipart(changedChildren == null ? multipart :
                        new MultipartContainerNode(multipart.Kind, changedChildren));
                    break;
                default:
                    throw new NotSupportedException("The body node type is not supported by this rewriter.");
            }
            return result ?? throw new InvalidOperationException("A body transformation must return a node.");
        }
    }
}