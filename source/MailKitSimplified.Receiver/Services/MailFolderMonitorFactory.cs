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
        private readonly IOptionsSnapshot<MailboxOptions> _mailboxOptionsSnapshot;
        private readonly ILoggerFactory _loggerFactory;

        public MailFolderMonitorFactory(IOptionsSnapshot<MailboxOptions> mailboxOptionsSnapshot, ILoggerFactory loggerFactory = null)
        {
            _mailboxOptionsSnapshot = mailboxOptionsSnapshot;
            _loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
        }

        public async Task MonitorAllMailboxesAsync(Action<IMessageSummary> action, CancellationToken cancellationToken = default)
        {
            var mailFolderMonitors = GetAllMailFolderMonitors();
            await Task.WhenAll(mailFolderMonitors.Select(m => m.OnMessageArrival(action).IdleAsync(cancellationToken)));
        }

        public IList<IMailFolderMonitor> GetAllMailFolderMonitors()
        {
            var mailFolderMonitors = new List<IMailFolderMonitor>();
            var folderMonitorOptions = _mailboxOptionsSnapshot?.Value.FolderMonitors;
            if (folderMonitorOptions != null)
            {
                foreach (var folderMonitor in folderMonitorOptions)
                {
                    var mailFolderMonitor = MailFolderMonitor.Create(folderMonitor, _loggerFactory.CreateLogger<MailFolderMonitor>());
                    mailFolderMonitors.Add(mailFolderMonitor);
                }
            }
            return mailFolderMonitors;
        }

        public void Dispose()
        {
            _loggerFactory.Dispose();
        }
    }
}