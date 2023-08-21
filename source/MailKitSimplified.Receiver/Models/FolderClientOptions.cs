using System.Collections.Generic;

namespace MailKitSimplified.Receiver.Models
{
    public class FolderClientOptions
    {
        public static readonly List<string> CommonSentFolderNames = new List<string> {
            "Sent", // [RFC-6154](https://www.rfc-editor.org/rfc/rfc6154#page-3), Thunderbird, Yahoo, Apple
            "Sent Items", // Outlook/365/Live/Hotmail
            "[Gmail]/Sent Mail", // Gmail
            "Sent Messages", // Mail on iOS
        };

        public static readonly List<string> CommonDraftsFolderNames = new List<string> {
            "Drafts", // [RFC-6154](https://www.rfc-editor.org/rfc/rfc6154#page-3), Thunderbird, Apple, iOS, Outlook/365/Live/Hotmail
            "[Gmail]/Drafts", // Gmail
            "Draft", // Yahoo
        };

        public static readonly List<string> CommonTrashFolderNames = new List<string> {
            "Trash", // [RFC-6154](https://www.rfc-editor.org/rfc/rfc6154#page-3)
            "Deleted Items", // Outlook/365/Live/Hotmail
            "[Gmail]/Trash", // Gmail
            "Trash", // Thunderbird, Yahoo
        };

        public static readonly List<string> CommonJunkFolderNames = new List<string> {
            "Junk", // [RFC-6154](https://www.rfc-editor.org/rfc/rfc6154#page-3), Thunderbird, Yahoo, Apple
            "Junk E-mail", // Outlook/365/Live/Hotmail
            "[Gmail]/Spam", // Gmail
        };

        public IList<string> SentFolderNames { get; set; } = CommonSentFolderNames;

        public IList<string> DraftsFolderNames { get; set; } = CommonDraftsFolderNames;

        public IList<string> JunkFolderNames { get; set; } = CommonJunkFolderNames;

        public IList<string> TrashFolderNames { get; set; } = CommonTrashFolderNames;
    }
}
