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
using System.Linq;

namespace MailKitSimplified.Receiver.Services
{
    public sealed class MailFolderMonitor : IMailFolderMonitor, IDisposable
    {
        private Func<IMessageSummary, Task> _messageArrivalMethod;
        private Func<IMessageSummary, Task> _messageDepartureMethod;
        private Func<IMessageSummary, Task> _messageFlagsChangedMethod;

        private static readonly Task _completedTask = Task.CompletedTask;
        private readonly object _cacheLock = new object();
        private readonly IList<IMessageSummary> _messageCache = new List<IMessageSummary>(); // this could be large
        private readonly ConcurrentQueue<IMessageSummary> _arrivalQueue = new ConcurrentQueue<IMessageSummary>();
        private readonly ConcurrentQueue<IMessageSummary> _departureQueue = new ConcurrentQueue<IMessageSummary>();
        private readonly ConcurrentQueue<IMessageSummary> _flagChangeQueue = new ConcurrentQueue<IMessageSummary>();
        private CancellationTokenSource _arrival = new CancellationTokenSource();
        private CancellationTokenSource _cancel;
        private IImapClient _imapClient;
        private IImapClient _fetchClient;
        private IMailFolder _mailFolder;
        private IMailFolder _fetchFolder;
        private bool _canIdle;

        private ILogger _logger;
        private readonly IImapReceiver _imapReceiver;
        private readonly IImapReceiver _fetchReceiver;
        private readonly FolderMonitorOptions _folderMonitorOptions;
        private readonly IList<TimeSpan> _retryTimeouts;

        public MailFolderMonitor(IImapReceiver imapReceiver, IOptions<FolderMonitorOptions> folderMonitorOptions = null, ILogger<MailFolderMonitor> logger = null)
        {
            _logger = logger ?? NullLogger<MailFolderMonitor>.Instance;
            _imapReceiver = imapReceiver?.Clone() ?? throw new ArgumentNullException(nameof(imapReceiver));
            _fetchReceiver = imapReceiver.Clone();
            _folderMonitorOptions = folderMonitorOptions?.Value ?? new FolderMonitorOptions();
            _retryTimeouts = _folderMonitorOptions.ExceptionRetryDelay.ToExponentialBackoff(
                _folderMonitorOptions.MaxRetries, _folderMonitorOptions.ExceptionRetryFactor, fastFirst: true).ToList();
            _messageArrivalMethod = (m) =>
            {
                _logger.Log<MailFolderMonitor>($"{_imapReceiver} message #{m.UniqueId} arrival processed.", LogLevel.Debug);
                return _completedTask;
            };
            _messageDepartureMethod = (m) =>
            {
                _logger.Log<MailFolderMonitor>($"{_imapReceiver} message #{m.UniqueId} departure processed.", LogLevel.Debug);
                return _completedTask;
            };
            _messageFlagsChangedMethod = (m) =>
            {
                _logger.Log<MailFolderMonitor>($"{_imapReceiver} message #{m.UniqueId} flag change processed.", LogLevel.Debug);
                return _completedTask;
            };
        }

        public static MailFolderMonitor Create(FolderMonitorOptions folderMonitorOptions, ILogger<MailFolderMonitor> logger = null, ILogger<ImapReceiver> logImap = null, IProtocolLogger protocolLogger = null, IImapClient imapClient = null)
        {
            if (folderMonitorOptions == null)
                throw new ArgumentNullException(nameof(folderMonitorOptions));
            var imapReceiver = ImapReceiver.Create(folderMonitorOptions.EmailReceiver, logImap, protocolLogger, imapClient);
            var options = Options.Create(folderMonitorOptions);
            var receiver = new MailFolderMonitor(imapReceiver, options, logger);
            return receiver;
        }

        public static MailFolderMonitor Create(EmailReceiverOptions emailReceiverOptions, ILogger<MailFolderMonitor> logger = null, ILogger<ImapReceiver> logImap = null, IProtocolLogger protocolLogger = null, IImapClient imapClient = null)
        {
            if (emailReceiverOptions == null)
                throw new ArgumentNullException(nameof(emailReceiverOptions));
            var folderMonitorOptions = new FolderMonitorOptions
            {
                EmailReceiver = emailReceiverOptions
            };
            var receiver = Create(folderMonitorOptions, logger, logImap, protocolLogger, imapClient);
            return receiver;
        }

        public static MailFolderMonitor Create(IImapClient imapClient, EmailReceiverOptions emailReceiverOptions, ILogger<MailFolderMonitor> logger = null, ILogger<ImapReceiver> logImap = null)
        {
            if (emailReceiverOptions == null)
                throw new ArgumentNullException(nameof(emailReceiverOptions));
            var folderMonitorOptions = new FolderMonitorOptions
            {
                EmailReceiver = emailReceiverOptions
            };
            var options = Options.Create(folderMonitorOptions);
            var imapReceiver = ImapReceiver.Create(imapClient, emailReceiverOptions, logImap);
            var mailFolderMonitor = new MailFolderMonitor(imapReceiver, options, logger);
            return mailFolderMonitor;
        }

        /// <summary>
        /// Logging setup for those not using dependency injection.
        /// </summary>
        public MailFolderMonitor SetLogger(ILogger logger)
        {
            if (logger != null)
                _logger = logger;
            return this;
        }

        /// <summary>
        /// Logging setup for those not using dependency injection.
        /// </summary>
        public MailFolderMonitor SetLogger(ILoggerFactory loggerFactory)
        {
            if (loggerFactory != null)
                _logger = loggerFactory.CreateLogger<MailFolderMonitor>();
            return this;
        }

        /// <summary>
        /// Logging setup for those not using dependency injection.
        /// </summary>
        public MailFolderMonitor SetLogger(Action<ILoggingBuilder> configure = null)
        {
            ILoggerFactory loggerFactory = null;
            if (configure != null)
                loggerFactory = LoggerFactory.Create(configure);
#if DEBUG
            else
                loggerFactory = LoggerFactory.Create(_ => _.SetMinimumLevel(LogLevel.Debug).AddDebug().AddConsole());
#endif
            return SetLogger(loggerFactory);
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

        public IMailFolderMonitor SetIgnoreExistingMailOnConnect(bool ignoreExisting = true)
        {
            _folderMonitorOptions.IgnoreExistingMailOnConnect = ignoreExisting;
            return this;
        }

        public IMailFolderMonitor SetMessageSummaryItems(MessageSummaryItems itemSelection = MessageSummaryItems.Envelope | MessageSummaryItems.BodyStructure | MessageSummaryItems.Flags)
        {
            _folderMonitorOptions.MessageSummaryItems = itemSelection; // | MessageSummaryItems.UniqueId;
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

        public IMailFolderMonitor OnMessageFlagsChanged(Func<IMessageSummary, Task> messageFlagsChangedMethod)
        {
            _messageFlagsChangedMethod = messageFlagsChangedMethod;
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

        public IMailFolderMonitor OnMessageFlagsChanged(Action<IMessageSummary> messageFlagsChangedMethod) =>
            OnMessageFlagsChanged((messageSummary) =>
            {
                messageFlagsChangedMethod(messageSummary);
                return _completedTask;
            });

        public async Task IdleAsync(CancellationToken cancellationToken = default, bool handleExceptions = true)
        {
            _logger.Log<MailFolderMonitor>($"{_imapReceiver} monitoring requested.", LogLevel.Trace);
            _cancel = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            try
            {
                var tasks = handleExceptions ? new Task[]
                {
                    IdleStartAsync(_cancel.Token).ContinueWith((t) =>
                        OnIdleException(t, "Idle client failed."), TaskContinuationOptions.OnlyOnFaulted),
                    ProcessArrivalQueueAsync(_messageArrivalMethod, _cancel.Token).ContinueWith(t =>
                        _logger.Log<MailFolderMonitor>(t.Exception?.GetBaseException(),
                            "Arrival queue processing failed."), TaskContinuationOptions.OnlyOnFaulted),
                    ProcessDepartureQueueAsync(_messageDepartureMethod, _cancel.Token).ContinueWith(t =>
                        _logger.Log<MailFolderMonitor>(t.Exception?.GetBaseException(),
                            "Departure queue processing failed."), TaskContinuationOptions.OnlyOnFaulted),
                    ProcessFlagChangeQueueAsync(_messageFlagsChangedMethod, _cancel.Token).ContinueWith(t =>
                        _logger.Log<MailFolderMonitor>(t.Exception?.GetBaseException(),
                            "Flag change queue processing failed."), TaskContinuationOptions.OnlyOnFaulted)
                } : new Task[] {
                    IdleStartAsync(_cancel.Token),
                    ProcessArrivalQueueAsync(_messageArrivalMethod, _cancel.Token),
                    ProcessDepartureQueueAsync(_messageDepartureMethod, _cancel.Token),
                    ProcessFlagChangeQueueAsync(_messageFlagsChangedMethod, _cancel.Token)
                };
                await Task.WhenAll(tasks).ConfigureAwait(false);
                _logger.Log<MailFolderMonitor>($"{_imapReceiver} monitoring complete.", LogLevel.Information);
            }
            catch (OperationCanceledException)
            {
                if (handleExceptions)
                    _logger.Log<MailFolderMonitor>($"{_imapReceiver} email monitoring cancelled.", LogLevel.Trace);
                else
                    throw;
            }
            catch (Exception ex)
            {
                if (handleExceptions)
                    _logger.Log<MailFolderMonitor>(ex, $"{_imapReceiver} email monitoring failed.");
                else
                    throw;
            }

            void OnIdleException(Task task, string message)
            {
                _logger.Log<MailFolderMonitor>(task.Exception?.GetBaseException(), message);
            }
        }

        private async Task IdleStartAsync(CancellationToken cancellationToken = default, bool handleExceptions = true)
        {
            if (_messageArrivalMethod != null && _messageDepartureMethod != null)
            {
                try
                {
                    _imapClient = await _imapReceiver.ConnectAuthenticatedImapClientAsync(cancellationToken).ConfigureAwait(false);
                    _canIdle = _imapClient.Capabilities.HasFlag(ImapCapabilities.Idle);
                    _fetchClient = await _fetchReceiver.ConnectAuthenticatedImapClientAsync(cancellationToken).ConfigureAwait(false);
                    _fetchFolder = await _fetchReceiver.ConnectMailFolderAsync(cancellationToken).ConfigureAwait(false);
                    _ = await _fetchFolder.OpenAsync(FolderAccess.ReadWrite, cancellationToken).ConfigureAwait(false);
                    _mailFolder = await _imapReceiver.ConnectMailFolderAsync(cancellationToken).ConfigureAwait(false);
                    _ = await _mailFolder.OpenAsync(FolderAccess.ReadOnly, cancellationToken).ConfigureAwait(false);
                    var connectOption = _folderMonitorOptions.IgnoreExistingMailOnConnect ? "ignoring" : "fetching";
                    _logger.Log<MailFolderMonitor>($"{_imapReceiver} ({_mailFolder.Count}) idle monitor started, {connectOption} existing emails.", LogLevel.Information);

                    _mailFolder.CountChanged += OnCountChanged;
                    _mailFolder.MessageExpunged += OnMessageExpunged;
                    _mailFolder.MessageFlagsChanged += OnFlagsChanged;

                    if (_mailFolder.Count > 0)
                        await ProcessMessagesArrivedAsync(true, cancellationToken).ConfigureAwait(false);
                    await WaitForNewMessagesAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    _logger.Log<MailFolderMonitor>($"{_imapReceiver} Initial fetch or idle task in mail folder monitor was cancelled.", LogLevel.Debug);
                }
                catch (AuthenticationException ex)
                {
                    _logger.Log<MailFolderMonitor>(ex, $"{_imapReceiver} Stopping mail folder monitor service.", LogLevel.Warning);
                }
                catch (InvalidOperationException ex)
                {
                    if (handleExceptions)
                        _logger.Log<MailFolderMonitor>(ex, $"{_imapReceiver} IMAP client not available.", LogLevel.Error);
                    else
                        throw;
                }
                finally
                {
                    Disconnect(throwOnFirstException: false);
                }
            }
        }

        private void Disconnect(bool throwOnFirstException)
        {
            _logger.Log<MailFolderMonitor>($"{_imapReceiver} Disconnecting IMAP idle client...", LogLevel.Trace);
            if (_mailFolder != null)
            {
                _mailFolder.MessageFlagsChanged += OnFlagsChanged;
                _mailFolder.MessageExpunged -= OnMessageExpunged;
                _mailFolder.CountChanged -= OnCountChanged;
            }

            _cancel?.Cancel(throwOnFirstException);
            _messageCache?.Clear();
#if NET5_0_OR_GREATER
            _arrivalQueue?.Clear();
            _departureQueue?.Clear();
            _flagChangeQueue?.Clear();
#endif
        }

        /// <exception cref="AuthenticationException">Failed to authenticate</exception>
        private async ValueTask ReconnectAsync(CancellationToken cancellationToken = default)
        {
            int attemptCount = 0;
            while (!cancellationToken.IsCancellationRequested && attemptCount < _folderMonitorOptions.MaxRetries)
            {
                try
                {
                    _ = await _imapReceiver.ConnectAuthenticatedImapClientAsync(cancellationToken).ConfigureAwait(false);
                    if (!_mailFolder.IsOpen)
                    {
                        _ = await _mailFolder.OpenAsync(FolderAccess.ReadOnly, cancellationToken).ConfigureAwait(false);
                        _logger.Log<MailFolderMonitor>($"{_mailFolder.FullName} mail folder re-opened with ReadOnly access.", LogLevel.Trace);
                    }
                    _ = await _fetchReceiver.ConnectAuthenticatedImapClientAsync(cancellationToken).ConfigureAwait(false);
                    if (!_fetchFolder.IsOpen)
                    {
                        _ = await _fetchFolder.OpenAsync(FolderAccess.ReadWrite, cancellationToken).ConfigureAwait(false);
                        _logger.Log<MailFolderMonitor>($"{_fetchFolder.FullName} mail folder re-opened with ReadWrite access.", LogLevel.Trace);
                    }
                    break;
                }
                catch (ImapProtocolException ex)
                {
                    await LogDelayAsync(ex, "IMAP protocol exception").ConfigureAwait(false);
                }
                catch (ImapCommandException ex)
                {
                    await LogDelayAsync(ex, "IMAP command exception").ConfigureAwait(false);
                }
                catch (SocketException ex)
                {
                    await LogDelayAsync(ex, "IMAP socket exception").ConfigureAwait(false);
                }
                catch (IOException ex)
                {
                    await LogDelayAsync(ex, "IMAP I/O exception").ConfigureAwait(false);
                }

                async ValueTask LogDelayAsync(Exception exception, string exceptionType)
                {
                    var backoffDelay = attemptCount <= _retryTimeouts.Count ?
                        _retryTimeouts[attemptCount] : _folderMonitorOptions.ExceptionRetryDelay;
                    bool isBackoff = attemptCount > 0 && attemptCount < _folderMonitorOptions.MaxRetries;
                    var backoff = isBackoff ? $", backing off for {backoffDelay} seconds" : string.Empty;
                    var message = $"{_imapReceiver} {exceptionType} during connection attempt #{++attemptCount}{backoff}.";
                    if (attemptCount < _folderMonitorOptions.MaxRetries)
                        _logger.Log<MailFolderMonitor>(message, LogLevel.Information);
                    else
                        throw exception; // TODO fix this, it changes the stacktrace parameter
                    if (isBackoff)
                        await Task.Delay(backoffDelay, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        private async ValueTask WaitForNewMessagesAsync(CancellationToken cancellationToken = default)
        {
            int retryCount = 0;
            do
            {
                try
                {
                    retryCount++;
                    if (_canIdle)
                    {
                        using (var done = new CancellationTokenSource(TimeSpan.FromMinutes(_folderMonitorOptions.IdleMinutes)))
                        using (var arrival = CancellationTokenSource.CreateLinkedTokenSource(_arrival.Token, done.Token))
                            await _imapClient.IdleAsync(arrival.Token, cancellationToken).ConfigureAwait(false);
                    }
                    else // simulate IMAP idle
                    {
                        await Task.Delay(TimeSpan.FromMinutes(1), cancellationToken).ConfigureAwait(false);
                        await _imapClient.NoOpAsync(cancellationToken).ConfigureAwait(false);
                    }
                    if (_arrival.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
                        await ProcessMessagesArrivedAsync(false, cancellationToken).ConfigureAwait(false);
                    retryCount = 0;
                }
                catch (OperationCanceledException) // includes TaskCanceledException
                {
                    _logger.Log<MailFolderMonitor>($"{_imapReceiver} Mail folder idle wait task cancelled.", LogLevel.Trace);
                    _cancel.Cancel(false);
                    break;
                }
                catch (ImapProtocolException ex)
                {
                    string error = ex.Message.TrimEnd(new char[] { ' ', '.' });
                    var message = $"{error}. IMAP protocol exception, reconnecting and trying again.";
                    if (ex.Message.StartsWith("Idle timeout"))
                        _logger.Log<MailFolderMonitor>(message, LogLevel.Debug);
                    else
                        _logger.Log<MailFolderMonitor>(message, LogLevel.Information);
                    if (_folderMonitorOptions.IdleMinutes > FolderMonitorOptions.IdleMinutesGmail)
                        _folderMonitorOptions.IdleMinutes = FolderMonitorOptions.IdleMinutesGmail;
                    else if (_folderMonitorOptions.IdleMinutes == FolderMonitorOptions.IdleMinutesGmail)
                        _folderMonitorOptions.IdleMinutes = 1;
                }
                catch (ImapCommandException)
                {
                    _logger.Log<MailFolderMonitor>($"{_imapReceiver} IMAP command exception, rechecking server connection.");
                }
                catch (IOException)
                {
                    _logger.Log<MailFolderMonitor>($"{_imapReceiver} IMAP I/O exception, reconnecting.");
                }
                catch (SocketException)
                {
                    _logger.Log<MailFolderMonitor>($"{_imapReceiver} IMAP socket exception, reconnecting.");
                }
                catch (ServiceNotConnectedException)
                {
                    _logger.Log<MailFolderMonitor>($"{_imapReceiver} IMAP service not connected, reconnecting.");
                }
                catch (ServiceNotAuthenticatedException)
                {
                    _logger.Log<MailFolderMonitor>($"{_imapReceiver} IMAP service not authenticated, authenticating.");
                }
                catch (InvalidOperationException ex)
                {
                    _logger.Log<MailFolderMonitor>(ex, $"{_imapReceiver} IMAP client is probably being accessed by multiple threads.");
                    _cancel.Cancel(false);
                    break;
                }
                finally
                {
                    if (!_cancel.IsCancellationRequested)
                        await ReconnectAsync(cancellationToken).ConfigureAwait(false);
                }
            } while (!cancellationToken.IsCancellationRequested && retryCount < _folderMonitorOptions.MaxRetries);
        }

        private async ValueTask<int> ProcessMessagesArrivedAsync(bool firstConnection = false, CancellationToken cancellationToken = default)
        {
            int startIndex = _messageCache.Count;
            await _fetchClient.NoOpAsync(cancellationToken).ConfigureAwait(false);
            _logger.Log<MailFolderMonitor>($"{_fetchReceiver} ({_fetchFolder.Count}) fetching new message arrivals, starting from {startIndex}.", LogLevel.Trace);
            if (startIndex > _fetchFolder.Count)
            {
                _logger.Log<MailFolderMonitor>($"{_fetchReceiver} start index {startIndex} is higher than fetched folder count of {_fetchFolder.Count}, monitored count is {_mailFolder.Count}.", LogLevel.Trace);
                startIndex = _fetchFolder.Count;
            }
            var filter = _folderMonitorOptions.MessageSummaryItems | MessageSummaryItems.UniqueId;
            var fetched = await _fetchFolder.FetchAsync(startIndex, -1, filter, cancellationToken).ConfigureAwait(false);
            if (_arrival.IsCancellationRequested)
                _arrival = new CancellationTokenSource();
            var newMail = _messageCache.TryAddUniqueRange(fetched);
            bool isIgnoreExistingMailOnConnect = firstConnection && !_folderMonitorOptions.IgnoreExistingMailOnConnect;
            if (!firstConnection || isIgnoreExistingMailOnConnect)
            {
                foreach (var mailItem in newMail)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;
                    _logger.Log<MailFolderMonitor>($"{_imapReceiver} message #{mailItem.UniqueId} arrived.", LogLevel.Debug);
                    _arrivalQueue.Enqueue(mailItem);
                }
            }
            return newMail.Count;
        }

        private async Task ProcessArrivalQueueAsync(Func<IMessageSummary, Task> messageArrivalMethod, CancellationToken cancellationToken = default)
        {
            int retryCount = 0;
            if (messageArrivalMethod != null)
            {
                IMessageSummary messageSummary = null;
                do
                {
                    retryCount++;
                    try
                    {
                        if (_arrivalQueue.TryDequeue(out messageSummary))
                            await messageArrivalMethod(messageSummary).ConfigureAwait(false);
                        else if (_arrivalQueue.IsEmpty)
                            await Task.Delay(_folderMonitorOptions.EmptyQueueMaxDelayMs, cancellationToken).ConfigureAwait(false);
                        retryCount = 0;
                    }
                    catch (OperationCanceledException)
                    {
                        _logger.Log<MailFolderMonitor>($"{_imapReceiver} Arrival queue cancelled.", LogLevel.Trace);
                        break;
                    }
                    catch (Exception ex)
                    {
                        if (messageSummary != null)
                            _arrivalQueue.Enqueue(messageSummary);
                        var backoff = _folderMonitorOptions.EmptyQueueMaxDelayMs * retryCount;
                        _logger.Log<MailFolderMonitor>(ex, $"{_imapReceiver} Error occurred processing arrival queue item ({messageSummary?.UniqueId}) during attempt #{retryCount}, backing off for {backoff}ms.", LogLevel.Warning);
                        await Task.Delay(backoff, cancellationToken).ConfigureAwait(false);
                    }
                }
                while (!cancellationToken.IsCancellationRequested && retryCount < _folderMonitorOptions.MaxRetries);
            }
        }

        private async Task ProcessDepartureQueueAsync(Func<IMessageSummary, Task> messageDepartureMethod, CancellationToken cancellationToken = default)
        {
            int retryCount = 0;
            if (messageDepartureMethod != null)
            {
                IMessageSummary messageSummary = null;
                do
                {
                    retryCount++;
                    try
                    {
                        if (_departureQueue.TryDequeue(out messageSummary))
                            await messageDepartureMethod(messageSummary).ConfigureAwait(false);
                        else if (_departureQueue.IsEmpty)
                            await Task.Delay(_folderMonitorOptions.EmptyQueueMaxDelayMs, cancellationToken).ConfigureAwait(false);
                        retryCount = 0;
                    }
                    catch (OperationCanceledException)
                    {
                        _logger.Log<MailFolderMonitor>($"{_imapReceiver} Departure queue cancelled.", LogLevel.Trace);
                        break;
                    }
                    catch (Exception ex)
                    {
                        if (messageSummary != null)
                            _departureQueue.Enqueue(messageSummary);
                        var backoff = _folderMonitorOptions.EmptyQueueMaxDelayMs * retryCount;
                        _logger.Log<MailFolderMonitor>(ex, $"{_imapReceiver} Error occurred processing departure queue item ({messageSummary?.UniqueId}) during attempt #{retryCount}, backing off for {backoff}ms.", LogLevel.Warning);
                        await Task.Delay(backoff, cancellationToken).ConfigureAwait(false);
                    }
                }
                while (!cancellationToken.IsCancellationRequested && retryCount < _folderMonitorOptions.MaxRetries);
            }
        }

        private async Task ProcessFlagChangeQueueAsync(Func<IMessageSummary, Task> messageFlagChangedMethod, CancellationToken cancellationToken = default)
        {
            int retryCount = 0;
            if (messageFlagChangedMethod != null)
            {
                IMessageSummary messageSummary = null;
                do
                {
                    retryCount++;
                    try
                    {
                        if (_flagChangeQueue.TryDequeue(out messageSummary))
                        {
                            var filter = _folderMonitorOptions.MessageSummaryItems | MessageSummaryItems.UniqueId;
                            var uniqueIds = new UniqueId[] { messageSummary.UniqueId };
                            var fetched = await _fetchFolder.FetchAsync(uniqueIds, filter, cancellationToken).ConfigureAwait(false);
                            messageSummary = fetched.FirstOrDefault();
                            if (messageSummary != null)
                                await messageFlagChangedMethod(messageSummary).ConfigureAwait(false);
                        }
                        else if (_flagChangeQueue.IsEmpty)
                            await Task.Delay(_folderMonitorOptions.EmptyQueueMaxDelayMs, cancellationToken).ConfigureAwait(false);
                        retryCount = 0;
                    }
                    catch (OperationCanceledException)
                    {
                        _logger.Log<MailFolderMonitor>($"{_imapReceiver} Flag change queue cancelled.", LogLevel.Trace);
                        break;
                    }
                    catch (Exception ex)
                    {
                        if (messageSummary != null)
                            _flagChangeQueue.Enqueue(messageSummary);
                        var backoff = _folderMonitorOptions.EmptyQueueMaxDelayMs * retryCount;
                        _logger.Log<MailFolderMonitor>(ex, $"{_imapReceiver} Error occurred processing flag change queue item ({messageSummary?.UniqueId}) during attempt #{retryCount}, backing off for {backoff}ms.", LogLevel.Warning);
                        await Task.Delay(backoff, cancellationToken).ConfigureAwait(false);
                    }
                }
                while (!cancellationToken.IsCancellationRequested && retryCount < _folderMonitorOptions.MaxRetries);
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
                        _logger.Log<MailFolderMonitor>($"{_imapReceiver} message count increased by {changeCount} ({previousCount} to {presentCount}).", LogLevel.Trace);
                    }
                    else if (changeCount < 0)
                    {
                        _logger.Log<MailFolderMonitor>($"{_imapReceiver} message count decreased by {changeCount} ({previousCount} to {presentCount}).", LogLevel.Trace);
                    }
                }
                else
                {
                    _logger.Log<MailFolderMonitor>($"{_imapReceiver} message count changed ({_messageCache.Count} to {_mailFolder.Count}), folder unknown.", LogLevel.Warning);
                }
            }
        }

        /// <summary>
        /// Keep the message cache in sync with the <see cref="ImapFolder">mail folder</see> by adding items.
        /// </summary>
        private void OnFlagsChanged(object sender, MessageFlagsChangedEventArgs e)
        {
            int index;
            int cachedCount;
            IMessageSummary messageSummary = null;
            lock (_cacheLock)
            {
                index = e.Index;
                cachedCount = _messageCache.Count;
                if (index < cachedCount)
                {
                     messageSummary = _messageCache[index];
                }
            }
            using (_logger.BeginScope("OnFlagsChanged"))
            {
                if (messageSummary != null)
                {
                    _flagChangeQueue.Enqueue(messageSummary);
                    _logger.Log<MailFolderMonitor>($"{_imapReceiver}[{index}] flags have changed ({e.Flags}), item #{messageSummary.UniqueId}.", LogLevel.Trace);
                }
                else
                {
                    _logger.Log<MailFolderMonitor>($"{_imapReceiver}[{index}] message flag change (count={cachedCount}) was out of range.", LogLevel.Warning);
                }
            }
        }

        /// <summary>
        /// Keep the message cache in sync with the <see cref="ImapFolder">mail folder</see> by removing items.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Collection index is invalid.</exception>
        private void OnMessageExpunged(object sender, MessageEventArgs e)
        {
            int index;
            int cachedCount;
            IMessageSummary messageSummary = null;
            lock (_cacheLock)
            {
                index = e.Index;
                cachedCount = _messageCache.Count;
                if (index < cachedCount)
                {
                    messageSummary = _messageCache[index];
                    _messageCache.RemoveAt(index);
                }
            }
            using (_logger.BeginScope("OnMessageExpunged"))
            {
                if (messageSummary != null)
                {
                    _departureQueue.Enqueue(messageSummary);
                    _logger.Log<MailFolderMonitor>($"{_imapReceiver}[{index}] (count={cachedCount}) expunged, item #{messageSummary.UniqueId}.", LogLevel.Trace);
                }
                else
                {
                    _logger.Log<MailFolderMonitor>($"{_imapReceiver}[{index}] (count={cachedCount}) was out of range.", LogLevel.Warning);
                }
            }
        }

        public MailFolderMonitor Copy() => MemberwiseClone() as MailFolderMonitor;

        public override string ToString() => _imapReceiver.ToString();

        public void Dispose()
        {
            _arrival?.Dispose();
            _cancel?.Dispose();
        }
    }
}
