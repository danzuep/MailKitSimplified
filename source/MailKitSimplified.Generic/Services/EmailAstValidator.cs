using System;
using System.Linq;
using MailKitSimplified.Generic.Abstractions;
using MailKitSimplified.Generic.Models;

namespace MailKitSimplified.Generic.Services
{
    /// <summary>Checks bounded structure without opening attachment content.</summary>
    public static class EmailAstValidator
    {
        /// <summary>Validates a content tree using per-operation visitor state.</summary>
        /// <param name="root">The root node.</param>
        /// <param name="maxDepth">Maximum depth, counting the root as one.</param>
        /// <param name="maxNodeCount">Maximum nodes, counting each occurrence.</param>
        /// <param name="maxTextCharacters">Maximum total header/body characters; excludes attachment bytes.</param>
        public static void Validate(IEmailAstNode root, int maxDepth = 32, int maxNodeCount = 1000,
            int maxTextCharacters = 1000000)
        {
            if (root == null)
                throw new ArgumentNullException(nameof(root));
            if (maxDepth < 1)
                throw new ArgumentOutOfRangeException(nameof(maxDepth));
            if (maxNodeCount < 1)
                throw new ArgumentOutOfRangeException(nameof(maxNodeCount));
            if (maxTextCharacters < 0)
                throw new ArgumentOutOfRangeException(nameof(maxTextCharacters));
            root.Accept(new ValidationVisitor(maxDepth, maxNodeCount, maxTextCharacters));
        }

        private sealed class ValidationVisitor : EmailAstWalker
        {
            private readonly int _maxDepth;
            private readonly int _maxNodeCount;
            private readonly int _maxTextCharacters;
            private int _depth;
            private int _nodeCount;
            private long _textCharacters;

            public ValidationVisitor(int maxDepth, int maxNodeCount, int maxTextCharacters)
            {
                _maxDepth = maxDepth;
                _maxNodeCount = maxNodeCount;
                _maxTextCharacters = maxTextCharacters;
            }

            public override void Visit(HeaderNode node) => Check(node);
            public override void Visit(TextBodyNode node) => Check(node);
            public override void Visit(HtmlBodyNode node) => Check(node);
            public override void Visit(AttachmentNode node) => Check(node);

            public override void Visit(MultipartContainerNode node)
            {
                Check(node);
                if (node.Kind == MultipartKind.Alternative && node.Children.Any(child => child is AttachmentNode))
                    throw new ArgumentException("Alternative parts must be body representations, not attachments.");
                if (node.Kind == MultipartKind.Related)
                {
                    if (node.Children[0] is AttachmentNode || node.Children.Skip(1).Any(child =>
                        !(child is AttachmentNode attachment) || !attachment.Attachment.IsInline))
                        throw new ArgumentException("Related content requires a root body followed by inline resources.");
                    var ids = node.Children.Skip(1).Cast<AttachmentNode>().Select(child => child.Attachment.ContentId);
                    if (ids.Distinct(StringComparer.Ordinal).Count() != node.Children.Count - 1)
                        throw new ArgumentException("Related resources must have unique content IDs.");
                }
                _depth++;
                try
                {
                    base.Visit(node);
                }
                finally
                {
                    _depth--;
                }
            }

            private void Check(IEmailAstNode node)
            {
                if (_depth + 1 > _maxDepth || ++_nodeCount > _maxNodeCount)
                    throw new ArgumentException("The email tree exceeds its depth or node-count limit.");
                switch (node)
                {
                    case HeaderNode header:
                        _textCharacters += (long)header.Name.Length + header.Value.Length;
                        break;
                    case TextBodyNode text:
                        _textCharacters += text.Text.Length;
                        break;
                    case HtmlBodyNode html:
                        _textCharacters += html.Html.Length;
                        break;
                }
                if (_textCharacters > _maxTextCharacters)
                    throw new ArgumentException("The email tree exceeds its text-size limit.");
            }
        }
    }
}