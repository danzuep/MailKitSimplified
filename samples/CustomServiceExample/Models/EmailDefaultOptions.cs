namespace CustomServiceExample.Models
{
    public class EmailDefaultOptions
    {
        public string FromAddress { get; set; } // noreply@example.com
        public string ReplyToAddress { get; set; } // noreply@example.com
        public string NoReplyAddress { get; set; }
        public IList<string> ToAddresses { get; set; }
        public IList<string> CcAddresses { get; set; }
        public IList<string> BccAddresses { get; set; }
        public string Subject { get; set; } = "Do Not Reply";
        public string BodyHtml { get; set; } = "<body>{{BodyText}}</body>{{Signature}}";
        public string BodyText { get; set; } = "";
        public string Signature { get; set; } = "";
        public IList<string> AttachmentFilePaths { get; set; }
    }
}
