using MailKit;
using MailKit.Security;
using MailKit.Net.Imap;
using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MailKitSimplified.Receiver.Abstractions;
using MailKitSimplified.Receiver.Extensions;
using MailKitSimplified.Receiver.Models;
using System.Net;

namespace MailKitSimplified.Receiver
{
    public sealed class MailFolderMonitor : IMailFolderMonitor
    {
        private Func<IMessageSummary, Task> _messageArrivalMethod;
        private Func<IMessageSummary, Task> _messageDepartureMethod;

        private static readonly Task _completedTask = Task.CompletedTask;
        private readonly object _cacheLock = new object();
        private readonly IList<IMessageSummary> _messageCache = new List<IMessageSummary>();
        private readonly ConcurrentQueue<IMessageSummary> _arrivalQueue = new ConcurrentQueue<IMessageSummary>();
        private readonly ConcurrentQueue<IMessageSummary> _departureQueue = new ConcurrentQueue<IMessageSummary>();
        private CancellationTokenSource _arrival = new CancellationTokenSource();
        private IImapClient _imapClient;
        private IMailFolder _mailFolder;
        private bool _canIdle;

        private readonly ILogger _logger;
        private readonly IImapReceiver _imapReceiver;
        private readonly FolderMonitorOptions _folderMonitorOptions;

        public MailFolderMonitor(IImapReceiver imapReceiver, IOptions<FolderMonitorOptions> folderMonitorOptions = null, ILogger<MailFolderMonitor> logger = null)
        {
            _logger = logger ?? NullLogger<MailFolderMonitor>.Instance;
            _imapReceiver = imapReceiver ?? throw new ArgumentNullException(nameof(imapReceiver));
            _folderMonitorOptions = folderMonitorOptions?.Value ?? new FolderMonitorOptions();
            _messageArrivalMethod = (m) =>
            {
                _logger.LogInformation($"{_imapReceiver} message #{m.UniqueId} arrival processed.");
                return _completedTask;
            };
            _messageDepartureMethod = (m) =>
            {
                _logger.LogInformation($"{_imapReceiver} message #{m.UniqueId} departure processed.");
                return _completedTask;
            };
        }

        public static MailFolderMonitor Create(IImapReceiver imapReceiver, MessageSummaryItems messageFilter = MessageSummaryItems.None, bool processMailOnConnect = true, byte idleMinutes = 9, byte maxRetries = 3, ILogger<MailFolderMonitor> logger = null)
        {
            var folderMonitorOptions = new FolderMonitorOptions
            {
                MessageFilter = messageFilter,
                ProcessMailOnConnect = processMailOnConnect,
                IdleMinutes = idleMinutes,
                MaxRetries = maxRetries
            };
            var mailFolderMonitor = Create(imapReceiver, folderMonitorOptions, logger);
            return mailFolderMonitor;
        }

        public static MailFolderMonitor Create(IImapReceiver imapReceiver, FolderMonitorOptions emailReceiverOptions, ILogger<MailFolderMonitor> logger = null)
        {
            var options = Options.Create(emailReceiverOptions);
            var mailFolderMonitor = new MailFolderMonitor(imapReceiver, options, logger);
            return mailFolderMonitor;
        }

        public MailFolderMonitor SetMessageFilter(MessageSummaryItems messageFilter = MessageSummaryItems.Envelope)
        {
            _folderMonitorOptions.MessageFilter = messageFilter;
            return this;
        }

        public MailFolderMonitor SetProcessMailOnConnect(bool processMailOnConnect = true)
        {
            _folderMonitorOptions.ProcessMailOnConnect = processMailOnConnect;
            return this;
        }

        public MailFolderMonitor SetIdleMinutes(byte idleMinutes = FolderMonitorOptions.IdleMinutesImap)
        {
            _folderMonitorOptions.IdleMinutes = idleMinutes;
            return this;
        }

        public MailFolderMonitor SetMaxRetries(byte maxRetries = 1)
        {
            _folderMonitorOptions.MaxRetries = maxRetries;
            return this;
        }

        public IMailFolderMonitor OnMessageArrival(Func<IMessageSummary, Task> messageArrivalMethod)
        {
            _messageArrivalMethod = messageArrivalMethod;
            return this;
        }

        public IMailFolderMonitor OnMessageDeparture(Func<IMessageSummary, Task> messageDepartureMethod)
        {
            _messageDepartureMethod = messageDepartureMethod;
            return this;
        }

        public IMailFolderMonitor OnMessageArrival(Action<IMessageSummary> messageArrivalMethod) =>
            OnMessageArrival((messageSummary) =>
            {
                messageArrivalMethod(messageSummary);
                return _completedTask;
            });

        public IMailFolderMonitor OnMessageDeparture(Action<IMessageSummary> messageDepartureMethod) =>
            OnMessageDeparture((messageSummary) =>
            {
                messageDepartureMethod(messageSummary);
                return _completedTask;
            });

        public async Task IdleAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var tasks = new Task[]
                {
                    IdleStartAsync(cancellationToken).ContinueWith(t =>
                        _logger.LogError(t.Exception?.InnerException ?? t.Exception,
                            "Idle client failed."), TaskContinuationOptions.OnlyOnFaulted),
                    ProcessArrivalQueueAsync(_messageArrivalMethod, cancellationToken).ContinueWith(t =>
                        _logger.LogError(t.Exception?.InnerException ?? t.Exception,
                            "Arrival queue processing failed."), TaskContinuationOptions.OnlyOnFaulted),
                    ProcessDepartureQueueAsync(_messageDepartureMethod, cancellationToken).ContinueWith(t =>
                        _logger.LogError(t.Exception?.InnerException ?? t.Exception,
                            "Departure queue processing failed."), TaskContinuationOptions.OnlyOnFaulted)
                };
                await Task.WhenAll(tasks).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                _logger.LogTrace($"{_imapReceiver} email monitoring cancelled.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{_imapReceiver} email monitoring failed.");
            }
        }

        private async Task IdleStartAsync(CancellationToken cancellationToken = default)
        {
            if (_messageArrivalMethod != null && _messageDepartureMethod != null)
            {
                try
                {
                    _imapClient = await _imapReceiver.ConnectAuthenticatedImapClientAsync(cancellationToken).ConfigureAwait(false);
                    _canIdle = _imapClient.Capabilities.HasFlag(ImapCapabilities.Idle);
                    _mailFolder = await _imapReceiver.ConnectMailFolderAsync(cancellationToken).ConfigureAwait(false);
                    _ = await _mailFolder.OpenAsync(FolderAccess.ReadOnly, cancellationToken).ConfigureAwait(false);
                    _logger.LogDebug($"{_imapReceiver} ({_mailFolder.Count}) idle monitor started.");

                    _mailFolder.CountChanged += OnCountChanged;
                    _mailFolder.MessageExpunged += OnMessageExpunged;

                    if (_mailFolder.Count > 0)
                        await ProcessMessagesArrivedAsync(true, cancellationToken).ConfigureAwait(false);
                    await WaitForNewMessagesAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogDebug("Initial fetch or idle task in mail folder monitor was cancelled.");
                }
                catch (AuthenticationException ex)
                {
                    _logger.LogWarning(ex, "Stopping mail folder monitor service.");
                }
                catch (InvalidOperationException ex)
                {
                    _logger.LogError(ex, "IMAP client not available.");
                }
                finally
                {
                    if (_mailFolder != null)
                    {
                        _mailFolder.MessageExpunged -= OnMessageExpunged;
                        _mailFolder.CountChanged -= OnCountChanged;
                    }

                    await _mailFolder.CloseAsync(false, CancellationToken.None).ConfigureAwait(false);
                    _mailFolder.Close(false, CancellationToken.None);
                    await _imapReceiver.DisconnectAsync(CancellationToken.None).ConfigureAwait(false);

                    _arrival?.Dispose();
                    _messageCache?.Clear();
#if NET6_0_OR_GREATER
                    _arrivalQueue?.Clear();
                    _departureQueue?.Clear();
#endif
                }
            }
        }
        
        private async ValueTask ReconnectAsync(CancellationToken cancellationToken = default)
        {
            _ = await _imapReceiver.ConnectAuthenticatedImapClientAsync(cancellationToken).ConfigureAwait(false);
            if (!_mailFolder.IsOpen)
            {
                _ = await _mailFolder.OpenAsync(FolderAccess.ReadOnly, cancellationToken).ConfigureAwait(false);
                _logger.LogTrace($"{_mailFolder.FullName} mail folder re-opened with ReadOnly access.");
            }
        }

        private async ValueTask WaitForNewMessagesAsync(CancellationToken cancellationToken = default)
        {
            int retryCount = 0;
            var cancel = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var idleTime = TimeSpan.FromMinutes(_folderMonitorOptions.IdleMinutes);
            do
            {
                try
                {
                    if (_canIdle)
                    {
                        using (var done = new CancellationTokenSource(idleTime))
                        using (var arrival = CancellationTokenSource.CreateLinkedTokenSource(_arrival.Token, done.Token))
                            await _imapClient.IdleAsync(arrival.Token, cancel.Token).ConfigureAwait(false);
                    }
                    else // simulate IMAP idle
                    {
                        await Task.Delay(TimeSpan.FromMinutes(1), cancel.Token).ConfigureAwait(false);
                        await _imapClient.NoOpAsync(cancel.Token).ConfigureAwait(false);
                    }
                    if (_arrival.IsCancellationRequested && !cancel.Token.IsCancellationRequested)
                        await ProcessMessagesArrivedAsync(false, cancel.Token).ConfigureAwait(false);
                    retryCount = 0;
                }
                catch (OperationCanceledException) // includes TaskCanceledException
                {
                    _logger.LogTrace($"{_imapReceiver} mail folder idle wait task cancelled.");
                    cancel.Cancel(false);
                }
                catch (ImapProtocolException ex)
                {
                    if (ex.Message.StartsWith("Idle timeout"))
                        _logger.LogInformation($"{ex.Message} Trying again.");
                    else
                        _logger.LogInformation(ex, "IMAP protocol exception, checking connection.");
                    await ReconnectAsync(cancel.Token).ConfigureAwait(false);
                    if (_folderMonitorOptions.IdleMinutes > FolderMonitorOptions.IdleMinutesGmail)
                        _folderMonitorOptions.IdleMinutes = FolderMonitorOptions.IdleMinutesGmail;
                    else if (_folderMonitorOptions.IdleMinutes == FolderMonitorOptions.IdleMinutesGmail)
                        _folderMonitorOptions.IdleMinutes = 1;
                }
                catch (ImapCommandException ex)
                {
                    _logger.LogInformation(ex, "IMAP command exception, rechecking server connection.");
                    if (!cancel.Token.IsCancellationRequested)
                        await ReconnectAsync(cancel.Token).ConfigureAwait(false);
                }
                catch (IOException)
                {
                    _logger.LogInformation("IMAP I/O exception, reconnecting.");
                    await ReconnectAsync(cancel.Token).ConfigureAwait(false);
                }
                catch (SocketException ex)
                {
                    _logger.LogInformation(ex, "IMAP socket exception.");
                    if (!cancel.Token.IsCancellationRequested)
                        await ReconnectAsync(cancel.Token).ConfigureAwait(false);
                }
                catch (ServiceNotConnectedException)
                {
                    _logger.LogInformation("IMAP service not connected, reconnecting.");
                    await ReconnectAsync(cancel.Token).ConfigureAwait(false);
                }
                catch (ServiceNotAuthenticatedException)
                {
                    _logger.LogInformation("IMAP service not authenticated, authenticating.");
                    await ReconnectAsync(cancel.Token).ConfigureAwait(false);
                }
                catch (InvalidOperationException ex)
                {
                    _logger.LogError(ex, "IMAP client is being accessed by multiple threads.");
                    cancel.Cancel(false);
                }
                finally
                {
                    retryCount++;
                }
            } while (!cancel.IsCancellationRequested && retryCount < _folderMonitorOptions.MaxRetries);
            cancel.Dispose();
        }

        private async ValueTask<int> ProcessMessagesArrivedAsync(bool firstConnection = false, CancellationToken cancellationToken = default)
        {
            int startIndex = _messageCache.Count;
            _logger.LogTrace($"{_imapReceiver} ({_mailFolder.Count}) Fetching new message arrivals, starting from {startIndex}.");
            if (startIndex > _mailFolder.Count)
                startIndex = _mailFolder.Count;
            var filter = _folderMonitorOptions.MessageFilter | MessageSummaryItems.UniqueId;
            var fetched = await _mailFolder.FetchAsync(startIndex, -1, filter, cancellationToken).ConfigureAwait(false);
            if (_arrival.IsCancellationRequested)
                _arrival = new CancellationTokenSource();
            var newMail = _messageCache.TryAddUniqueRange(fetched);
            if (!firstConnection || (firstConnection && _folderMonitorOptions.ProcessMailOnConnect))
                newMail.ActionEach((mail) => _arrivalQueue.Enqueue(mail), cancellationToken);
            return newMail.Count;
        }

        private async Task ProcessArrivalQueueAsync(Func<IMessageSummary, Task> messageArrivalMethod, CancellationToken cancellationToken = default)
        {
            IMessageSummary messageSummary = null;
            try
            {
                if (messageArrivalMethod != null)
                {
                    do
                    {
                        if (_arrivalQueue.TryDequeue(out messageSummary))
                            await messageArrivalMethod(messageSummary).ConfigureAwait(false);
                        else if (_arrivalQueue.IsEmpty)
                            await Task.Delay(100).ConfigureAwait(false);
                    }
                    while (!cancellationToken.IsCancellationRequested);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogTrace("Arrival queue cancelled.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error occurred processing arrival queue item #{messageSummary}.");
                if (messageSummary != null)
                    _arrivalQueue.Enqueue(messageSummary);
            }
        }

        private async Task ProcessDepartureQueueAsync(Func<IMessageSummary, Task> messageDepartureMethod, CancellationToken cancellationToken = default)
        {
            IMessageSummary messageSummary = null;
            try
            {
                if (messageDepartureMethod != null)
                {
                    do
                    {
                        if (_departureQueue.TryDequeue(out messageSummary))
                            await messageDepartureMethod(messageSummary).ConfigureAwait(false);
                        else if (_departureQueue.IsEmpty)
                            await Task.Delay(100).ConfigureAwait(false);
                    }
                    while (!cancellationToken.IsCancellationRequested);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogTrace("Departure queue cancelled.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error occurred processing departure queue item #{messageSummary}.");
                if (messageSummary != null)
                    _departureQueue.Enqueue(messageSummary);
            }
        }

        /// <summary>
        /// Keep the message cache in sync with the <see cref="ImapFolder">mail folder</see> by adding items.
        /// </summary>
        private void OnCountChanged(object sender, EventArgs e)
        {
            using (_logger.BeginScope("OnCountChanged"))
            {
                if (sender is ImapFolder folder)
                {
                    int previousCount = _messageCache.Count;
                    int presentCount = folder.Count;
                    int changeCount = presentCount - previousCount;
                    if (changeCount > 0)
                    {
                        _arrival?.Cancel(true);
                        _logger.LogTrace($"{_imapReceiver} message count increased by {changeCount} ({previousCount} to {presentCount}).");
                    }
                    else if (changeCount < 0)
                    {
                        _logger.LogTrace($"{_imapReceiver} message count decreased by {changeCount} ({previousCount} to {presentCount}).");
                    }
                }
                else
                {
                    _logger.LogWarning($"{_imapReceiver} message count changed ({_messageCache.Count} to {_mailFolder.Count}), folder unknown.");
                }
            }
        }

        /// <summary>
        /// Keep the message cache in sync with the <see cref="ImapFolder">mail folder</see> by removing items.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Collection index is invalid.</exception>
        private void OnMessageExpunged(object sender, MessageEventArgs e)
        {
            using (_logger.BeginScope("OnMessageExpunged"))
            {
                int index = e.Index;
                var cachedCount = _messageCache.Count;
                if (index < cachedCount)
                {
                    IMessageSummary messageSummary;
                    lock (_cacheLock)
                    {
                        messageSummary = _messageCache[index];
                        _messageCache.RemoveAt(index);
                    }
                    _logger.LogTrace($"{_imapReceiver}[{index}] (count={cachedCount}) expunged, item #{messageSummary.UniqueId}.");
                }
                else
                    _logger.LogWarning($"{_imapReceiver}[{index}] (count={cachedCount}) was out of range.");
            }
        }

        public override string ToString() => _imapReceiver.ToString();
    }
}
