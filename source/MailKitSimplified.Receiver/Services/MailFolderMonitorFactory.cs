using MailKit;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MailKitSimplified.Receiver.Abstractions;
using MailKitSimplified.Receiver.Models;

namespace MailKitSimplified.Receiver.Services
{
    public class MailFolderMonitorFactory : IMailFolderMonitorFactory
    {
        private readonly IOptionsMonitor<MailboxOptions> _mailboxOptions;
        private readonly IOptionsMonitor<EmailReceiverOptions> _receiverOptions;
        private readonly ILoggerFactory _loggerFactory;

        public MailFolderMonitorFactory(IOptionsMonitor<MailboxOptions> mailboxOptions, IOptionsMonitor<EmailReceiverOptions> receiverOptions = null, ILoggerFactory loggerFactory = null)
        {
            _mailboxOptions = mailboxOptions ?? throw new ArgumentNullException(nameof(mailboxOptions));
            _receiverOptions = receiverOptions;
            _loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
        }

        public async Task MonitorAllMailboxesAsync(Action<IMessageSummary> action, CancellationToken cancellationToken = default)
        {
            var mailFolderMonitors = GetAllMailFolderMonitors();
            await Task.WhenAll(mailFolderMonitors
                .Select(m => m.OnMessageArrival(action)
                    .IdleAsync(cancellationToken)));
        }

        public async Task MonitorAllMailboxesAsync(Func<IMessageSummary, Task> function, CancellationToken cancellationToken = default)
        {
            var mailFolderMonitors = GetAllMailFolderMonitors();
            await Task.WhenAll(mailFolderMonitors
                .Select(m => m.OnMessageArrival(function)
                    .IdleAsync(cancellationToken)));
        }

        public IList<IMailFolderMonitor> GetAllMailFolderMonitors()
        {
            var mailFolderMonitors = new List<IMailFolderMonitor>();
            var folderMonitorOptions = _mailboxOptions.CurrentValue.FolderMonitors;
            if (folderMonitorOptions != null)
            {
                foreach (var folderMonitor in folderMonitorOptions)
                {
                    if (folderMonitor == null)
                        continue;
                    if (string.IsNullOrEmpty(folderMonitor.EmailReceiver?.ImapHost) && _receiverOptions != null)
                        folderMonitor.EmailReceiver = _receiverOptions.CurrentValue;
                    if (folderMonitor.EmailReceiver.MailFolderNames.Any())
                    {
                        foreach (var mailFolderName in folderMonitor.EmailReceiver.MailFolderNames)
                        {
                            var options = folderMonitor.Copy();
                            if (string.IsNullOrEmpty(options.EmailReceiver.ImapHost) && _receiverOptions != null)
                                options.EmailReceiver = _receiverOptions.CurrentValue.Copy();
                            else if (options.EmailReceiver != folderMonitor.EmailReceiver)
                                options.EmailReceiver = folderMonitor.EmailReceiver.Copy();
                            options.EmailReceiver.MailFolderName = mailFolderName;
                            var mailFolderMonitor = MailFolderMonitor.Create(options, _loggerFactory.CreateLogger<MailFolderMonitor>());
                            mailFolderMonitors.Add(mailFolderMonitor);
                        }
                    }
                    else
                    {
                        var mailFolderMonitor = MailFolderMonitor.Create(folderMonitor, _loggerFactory.CreateLogger<MailFolderMonitor>());
                        mailFolderMonitors.Add(mailFolderMonitor);
                    }
                }
            }
            return mailFolderMonitors;
        }

        // Let the Garbage Collector dispose of the injected LoggerFactory.
    }
}