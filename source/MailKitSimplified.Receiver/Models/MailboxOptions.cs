using System;
using System.Collections.Generic;
using System.Linq;

namespace MailKitSimplified.Receiver.Models
{
    public class MailboxOptions
    {
        public const string SectionName = "Mailbox";

        private IList<EmailReceiverOptions> _emailReceivers = null;
        public IList<EmailReceiverOptions> EmailReceivers
        {
            get => _emailReceivers ?? FolderMonitors?.Select(f => f.EmailReceiver).ToList();
            set => _emailReceivers = value;
        }

        public IList<FolderMonitorOptions> FolderMonitors { get; set; } = null;

        public TimeSpan? SlidingCacheTime { get; set; } = null;

        public TimeSpan? MaximumCacheTime { get; set; } = null;

        public MailboxOptions Copy() => MemberwiseClone() as MailboxOptions;

        public override string ToString() =>
             $"{_emailReceivers?.Count} EmailReceivers, {FolderMonitors?.Count} FolderMonitors, SlidingCacheTime={SlidingCacheTime}, MaximumCacheTime={MaximumCacheTime}.";
    }
}
