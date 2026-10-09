using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using MailKitSimplified.Generic.Abstractions;

namespace MailKitSimplified.Generic.Models
{
    public class GenericEmail : IGenericEmail
    {
        public IDictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();

        public IList<IGenericEmailContact> From { get; set; } = new List<IGenericEmailContact>();

        public IList<IGenericEmailContact> ReplyTo { get; set; } = new List<IGenericEmailContact>();

        public IList<IGenericEmailContact> To { get; set; } = new List<IGenericEmailContact>();

        public IList<IGenericEmailContact> Cc { get; set; } = new List<IGenericEmailContact>();

        public IList<IGenericEmailContact> Bcc { get; set; } = new List<IGenericEmailContact>();

        public IDictionary<string, object> Attachments { get; set; } = new Dictionary<string, object>();

        public IEnumerable<string> AttachmentFilePaths => Attachments.Where(a => a.Value == null).Select(a => a.Key);

        public IEnumerable<string> AttachmentFileNames => AttachmentFilePaths.Select(a => Path.GetFileName(a));

        public string Subject { get; set; } = string.Empty;

        private BodyNode _body;

        /// <summary>Gets or sets the composed body, excluding dictionary attachments.</summary>
        public BodyNode Body
        {
            get => _body;
            set => _body = value ?? throw new ArgumentNullException(nameof(value));
        }

        public string BodyText
        {
            get => FindBody<TextBodyNode>(_body)?.Text ?? string.Empty;
            set => SetBody(new TextBodyNode(value ?? string.Empty), false);
        }

        public string BodyHtml
        {
            get => FindBody<HtmlBodyNode>(_body)?.Html ?? string.Empty;
            set => SetBody(new HtmlBodyNode(value ?? string.Empty), true);
        }

        private static TNode FindBody<TNode>(BodyNode node) where TNode : BodyNode
        {
            if (node is TNode match)
                return match;
            if (node is MultipartContainerNode multipart)
                foreach (var child in multipart.Children)
                {
                    var result = FindBody<TNode>(child);
                    if (result != null)
                        return result;
                }
            return null;
        }

        private void SetBody(BodyNode replacement, bool isHtml)
        {
            bool replaced = false;
            var body = ReplaceBody(_body, replacement, isHtml, ref replaced);
            if (replaced)
                _body = body;
            else if (body == null)
                _body = replacement;
            else
                _body = new MultipartContainerNode(MultipartKind.Alternative,
                    isHtml ? new[] { body, replacement } : new[] { replacement, body });
        }

        private static BodyNode ReplaceBody(BodyNode node, BodyNode replacement, bool isHtml, ref bool replaced)
        {
            if ((isHtml && node is HtmlBodyNode) || (!isHtml && node is TextBodyNode))
            {
                replaced = true;
                return replacement;
            }
            if (node is MultipartContainerNode multipart)
            {
                var children = new List<BodyNode>(multipart.Children.Count);
                foreach (var child in multipart.Children)
                    children.Add(ReplaceBody(child, replacement, isHtml, ref replaced));
                return new MultipartContainerNode(multipart.Kind, children);
            }
            return node;
        }

        public override string ToString()
        {
            string envelope = string.Empty;
            using (var text = new StringWriter())
            {
                text.WriteLine("Date: {0}", DateTimeOffset.Now);
                if (From.Count > 0)
                    text.WriteLine("From: {0}", string.Join("; ", From));
                if (To.Count > 0)
                    text.WriteLine("To: {0}", string.Join("; ", To));
                if (Cc.Count > 0)
                    text.WriteLine("Cc: {0}", string.Join("; ", Cc));
                if (Bcc.Count > 0)
                    text.WriteLine("Bcc: {0}", string.Join("; ", Bcc));
                text.WriteLine("Subject: {0}", Subject);
                if (Attachments.Count > 0)
                    text.WriteLine("{0} Attachment{1}: '{2}'",
                        Attachments.Count, Attachments.Count == 1 ? "" : "s",
                        string.Join("', '", Attachments.Keys));
                envelope = text.ToString();
            }
            return envelope;
        }
    }
}
