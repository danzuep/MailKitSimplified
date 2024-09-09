using System;
using MailKit;

namespace MailKitSimplified.Receiver.Models
{
    public class FolderMonitorOptions
    {
        public const string SectionName = "FolderMonitor";
        public const int IdleMinutesGmail = 9;
        public const int IdleMinutesImap = 29;

        /// <summary>
        /// Specify custom email receiver options.
        /// </summary>
        public EmailReceiverOptions EmailReceiver { get; set; } = null;

        /// <summary>
        /// Specify whether to process existing messages, default is false.
        /// </summary>
        public bool ProcessMailOnConnect { get; set; } = false;

        /// <summary>
        /// Specify whether to ignore existing messages, processing emails on connect is enabled by default.
        /// </summary>
        public bool IgnoreExistingMailOnConnect { get; set; } = false;

        /// <summary>
        /// Specify which properties of <see cref="IMessageSummary"/> should be populated other than <see cref="UniqueId"/>.
        /// </summary>
        public MessageSummaryItems MessageSummaryItems { get; set; } = MessageSummaryItems.None;

        /// <summary>
        /// Specify length of time to idle for, default is 9 minutes.
        /// </summary>
        public byte IdleMinutes { get; set; } = IdleMinutesGmail;

        /// <summary>
        /// Specify number of times to retry on failure, default is 3 times.
        /// </summary>
        public byte MaxRetries { get; set; } = 3;

        /// <summary>
        /// The length of time the message queues will idle for when empty and retry time between attempts after an exception.
        /// </summary>
        public ushort EmptyQueueMaxDelayMs { get; set; } = 500;

        /// <summary>
        /// The duration to wait for before the first retry after a failure.
        /// </summary>
        public TimeSpan ExceptionRetryDelay { get; set; } = TimeSpan.FromSeconds(2);

        /// <summary>
        /// The exponent to multiply each subsequent duration by after each failure.
        /// </summary>
        public double ExceptionRetryFactor { get; set; } = 2.0d;

        public override string ToString() => $"MessageSummaryItems={MessageSummaryItems}, IgnoreExisting={IgnoreExistingMailOnConnect}, IdleMinutes={IdleMinutes}, MaxRetries={MaxRetries}.";
    }
}
