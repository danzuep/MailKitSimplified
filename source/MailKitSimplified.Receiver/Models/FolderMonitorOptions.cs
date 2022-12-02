using MailKit;

namespace MailKitSimplified.Receiver.Models
{
    public class FolderMonitorOptions
    {
        public const string SectionName = "FolderMonitor";
        public const int IdleMinutesGmail = 9;
        public const int IdleMinutesImap = 29;

        /// <summary>
        /// Specify whether to process existing messages, default is false.
        /// </summary>
        public bool ProcessMailOnConnect { get; set; } = false;

        /// <summary>
        /// Specify which properties of <see cref="IMessageSummary"/> should be populated.
        /// <see cref="UniqueId"/> is always included by default.
        /// </summary>
        public MessageSummaryItems MessageFilter { get; set; } = MessageSummaryItems.None;

        /// <summary>
        /// Specify length of time to idle for, default is 9 minutes.
        /// </summary>
        public byte IdleMinutes { get; set; } = IdleMinutesGmail;

        /// <summary>
        /// Specify number of times to retry on failure, default is 3 times.
        /// </summary>
        public byte MaxRetries { get; set; } = 3;

        public override string ToString() => $"ProcessMailOnConnect={ProcessMailOnConnect}, MessageFilter={MessageFilter}, IdleMinutes={IdleMinutes}, MaxRetries={MaxRetries}.";
    }
}
