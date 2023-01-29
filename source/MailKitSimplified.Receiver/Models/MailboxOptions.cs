using System;
using System.Collections.Generic;
using MailKitSimplified.Receiver.Extensions;

namespace MailKitSimplified.Receiver.Models
{
    public class MailboxOptions
    {
        public const string SectionName = "Mailbox";

        public IList<EmailReceiverOptions> EmailReceivers { get; set; } = null;

        public IList<FolderMonitorOptions> FolderMonitors { get; set; } = null;

        public TimeSpan? SlidingCacheTime { get; set; } = null;

        public TimeSpan? MaximumCacheTime { get; set; } = null;

        public MailboxOptions Copy() => MemberwiseClone() as MailboxOptions;

        public override string ToString() => EmailReceivers.ToEnumeratedString(Environment.NewLine);
    }
}
