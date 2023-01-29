using MailKit;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace MailKitSimplified.Receiver.Abstractions
{
    public interface IMailFolderMonitorFactory : IDisposable
    {
        IList<IMailFolderMonitor> GetAllMailFolderMonitors();
        Task MonitorAllMailboxesAsync(Action<IMessageSummary> action, CancellationToken cancellationToken = default);
    }
}