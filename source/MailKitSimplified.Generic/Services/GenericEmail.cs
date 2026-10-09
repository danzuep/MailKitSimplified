using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using MailKitSimplified.Generic.Abstractions;

namespace MailKitSimplified.Generic.Services
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

        public string BodyText { get; set; } = string.Empty;

        public string BodyHtml { get; set; } = string.Empty;

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
