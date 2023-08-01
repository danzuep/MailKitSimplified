using System.Collections.Generic;

namespace MailKitSimplified.Receiver.Models
{
    public class FolderClientOptions
    {
        public static readonly List<string> CommonSentFolderNames = new List<string> {
            "Sent Items",
            "Sent Mail",
            "Sent Messages"
        };

        public IList<string> SentFolderNames { get; set; } = CommonSentFolderNames;
    }
}
