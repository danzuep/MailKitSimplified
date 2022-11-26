using MailKit;
using MailKit.Security;
using MailKit.Net.Imap;
using System;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using MailKitSimplified.Receiver.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using MailKitSimplified.Receiver.Extensions;

namespace MailKitSimplified.Receiver
{
    public delegate ValueTask MessagesArrived(IList<IMessageSummary> messages);

    /// <summary>
	/// Email idle client receiver. See also:
    /// <seealso href="https://github.com/jstedfast/MailKit/blob/master/Documentation/Examples/ImapIdleExample.cs">IdleClient.cs by Jeffrey Stedfast</seealso>.
    /// </summary>
    public sealed class IdleClientReceiver : IIdleClientReceiver
    {
		#region Private Fields
		const int _maxRetries = 3;
		private bool _messagesArrived;
		private bool _processingMessages;
		private CancellationTokenSource _cancel = new CancellationTokenSource();
        private CancellationTokenSource _done = new CancellationTokenSource();
		private IList<IMessageSummary> _messageCache = new List<IMessageSummary>();
		private MessagesArrived ProcessMessageSummariesAsync;
        private static readonly string _dateTimeFormat = "yyyy'-'MM'-'dd' 'HH':'mm':'ss"; //:s has 'T'
		private IMailFolder _mailFolder;
        private IMailFolderClient _mailFolderClient;
        private IImapClient _imapClient;
        private bool _canIdle;
        #endregion

        private readonly ILogger _logger;
		private readonly IImapReceiver _imapReceiver;

        public IdleClientReceiver(IImapReceiver imapReceiver, ILogger<IdleClientReceiver> logger = null)
		{
			_logger = logger ?? NullLogger<IdleClientReceiver>.Instance;
			_imapReceiver = imapReceiver ?? throw new ArgumentNullException(nameof(imapReceiver));
        }

        public async Task MonitorAsync(MessagesArrived messagesArrivedMethod, CancellationToken cancellationToken = default)
		{
			try
            {
				ProcessMessageSummariesAsync = messagesArrivedMethod;
				using (_imapClient = await _imapReceiver.ConnectImapClientAsync(cancellationToken))
				using (_mailFolderClient = await _imapReceiver.ConnectMailFolderClientAsync(cancellationToken))
				{
					_canIdle = _imapClient.Capabilities.HasFlag(ImapCapabilities.Idle);
					_mailFolder = await _mailFolderClient.ConnectAsync(false, cancellationToken);

					_mailFolder.CountChanged += OnCountChanged;
					_mailFolder.MessageExpunged += OnMessageExpunged;
					_mailFolder.MessageFlagsChanged += OnMessageFlagsChanged;

					_messagesArrived = _mailFolder.Count > 0;
					_logger.LogInformation("Email idle client started watching '{0}' ({1}) {2}.",
						_mailFolder.FullName, _mailFolder.Count, DateTime.Now.ToString(_dateTimeFormat));
					await IdleAsync(cancellationToken);
					_logger.LogInformation("Email idle client finished watching '{0}' ({1}) {2}.",
						_mailFolder.FullName, _mailFolder.Count, DateTime.Now.ToString(_dateTimeFormat));
					_messageCache.Clear();

					_mailFolder.MessageFlagsChanged -= OnMessageFlagsChanged;
					_mailFolder.MessageExpunged -= OnMessageExpunged;
					_mailFolder.CountChanged -= OnCountChanged;
                }

                await _imapReceiver.DisposeAsync();
            }
			catch (ServiceNotConnectedException ex)
			{
				_logger.LogError(ex, ex.Message);
				return;
			}
			catch (InvalidOperationException ex)
			{
				_logger.LogError(ex, "IMAP client is busy.");
				return;
			}
		}

        private async ValueTask ReconnectAsync() => _ = await _mailFolderClient.ConnectAsync(true, _cancel.Token).ConfigureAwait(false);

        async Task<int> ProcessMessagesAsync()
		{
			IList<IMessageSummary> fetched = new List<IMessageSummary>();
			int retryCount = 0;

			do
			{
				try
				{
					if (!_cancel.IsCancellationRequested)
					{
						int startIndex = _messageCache.Count;
                        _logger.LogTrace("'{0}' ({1}): Fetching new message arrivals, starting from {2}.",
                            _mailFolder.FullName, _mailFolder.Count, startIndex);
						if (startIndex > _mailFolder.Count)
							startIndex = _mailFolder.Count;
                        fetched = await _mailFolder.FetchAsync(startIndex, -1, MessageSummaryItems.UniqueId, _cancel.Token);
                        AddFetchedMessages(fetched);
                        _logger.LogDebug("Downloading {0} from '{1}' ({2}) at {3:HH':'mm':'ss'.'fff}. ID(s): {4}",
							fetched.Count, _mailFolder.FullName, _mailFolder.Count, DateTime.Now,
							fetched.Select(m => m.UniqueId).ToEnumeratedString());
						await ProcessMessageSummariesAsync(fetched);
					}
					else
						_logger.LogInformation("Service was cancelled during fetch.");
					break;
				}
				catch (ImapCommandException ex)
				{
					// command exceptions often result in the client getting disconnected
					_logger.LogWarning(ex, "Client request to examine 'INBOX' was denied, server unavailable. Reconnecting.");
					await ReconnectAsync().ConfigureAwait(false);
				}
				catch (ImapProtocolException)
				{
					// protocol exceptions often result in the client getting disconnected
					_logger.LogDebug("Imap protocol exception, reconnecting.");
					await ReconnectAsync().ConfigureAwait(false);
				}
				catch (IOException)
				{
					// I/O exceptions always result in the client getting disconnected
					_logger.LogDebug("Imap I/O exception, reconnecting.");
					await ReconnectAsync().ConfigureAwait(false);
				}
				catch (OperationCanceledException) // includes TaskCanceledException
				{
					_logger.LogDebug("Fetch task cancelled in email IdleClient.");
					RemoveFetchedMessages(fetched);
				}
				catch (Exception ex)
				{
					_logger.LogWarning(ex, "Imap idle client failed to fetch new messages.");
					RemoveFetchedMessages(fetched);
					throw;
				}
				finally
				{
					retryCount++;
					if (retryCount >= _maxRetries)
						RemoveFetchedMessages(fetched);
				}
			} while (retryCount < _maxRetries);

			int processedCount = fetched.Count;
			if (fetched.Count > 0)
				_logger.LogTrace("{0} message(s) processed.", processedCount);

			return processedCount;
		}

		private void AddFetchedMessages(IList<IMessageSummary> items, bool allowReprocess = true)
		{
			if (items != null)
				foreach (var item in items)
					if (allowReprocess || !_messageCache.Contains(item))
						_messageCache.Add(item);
		}

		private void RemoveFetchedMessages(IList<IMessageSummary> items)
        {
			if (items != null)
				foreach (var item in items)
					_messageCache.Remove(item);
		}

		async Task WaitForNewMessagesAsync()
		{
			do
			{
				try
				{
					if (_canIdle)
					{
						// Note: IMAP servers are only supposed to drop the connection after 30 minutes, so normally
						// we'd IDLE for a max of, say, ~29 minutes... but GMail seems to drop idle connections after
						// about 10 minutes, so we'll only idle for 9 minutes.
						_done = new CancellationTokenSource(TimeSpan.FromMinutes(9));
						var cancel = CancellationTokenSource.CreateLinkedTokenSource(_cancel.Token);

						_logger.LogTrace("Idle wait task started {0:HH':'mm':'ss'.'fff}.", DateTime.Now);

						await _imapClient.IdleAsync(_done.Token, cancel.Token);
						//_logger.LogTrace("Idle wait task finished, done: {0}, cancel: {1}, {2:HH':'mm':'ss'.'fff}.",
						//	_done.Token.IsCancellationRequested, cancel.Token.IsCancellationRequested, DateTime.Now);

						if (_done.IsCancellationRequested && _messageCache.Count > 0 && _mailFolder?.Count > 0 && _mailFolder.Count != _messageCache.Count)
						{
							_logger.LogInformation("'{0}' ({1}) count resynchronised from {2} back to 0.",
								_mailFolder?.FullName, _mailFolder?.Count, _messageCache.Count);
							_messageCache.Clear();
						}
						else if (_done.IsCancellationRequested)
						{
							_logger.LogTrace("'{0}' ({1}) idle client count is {2} on reset.",
								_mailFolder?.FullName, _mailFolder?.Count, _messageCache.Count);
						}

						_done.Dispose();
						_done = null;
					}
					else
					{
						// Use SMTP NOOP commands to simulate the IMAP idle capability, but don't spam it.
						await Task.Delay(TimeSpan.FromMinutes(1), _cancel.Token);
						await _imapClient.NoOpAsync(_cancel.Token);
					}
				}
				catch (ServiceNotConnectedException)
				{
					_logger.LogInformation("IMAP service not connected, reconnecting.");
					await ReconnectAsync().ConfigureAwait(false);
				}
				catch (ImapProtocolException)
				{
					// protocol exceptions often result in the client getting disconnected
					_logger.LogInformation("IMAP protocol exception, reconnecting.");
					await ReconnectAsync().ConfigureAwait(false);
				}
				catch (IOException)
				{
					// I/O exceptions always result in the client getting disconnected
					_logger.LogInformation("IMAP I/O exception, reconnecting.");
					await ReconnectAsync().ConfigureAwait(false);
				}
				catch (ImapCommandException ex)
				{
					// command exceptions often result in the client getting disconnected
					_logger.LogWarning(ex, "Client request to examine 'INBOX' was denied, server unavailable. Reconnecting.");
					await ReconnectAsync().ConfigureAwait(false);
				}
				catch (ObjectDisposedException)
				{
					_logger.LogWarning("Cancellation token object disposed.");
					_cancel.Token.ThrowIfCancellationRequested();
				}
				catch (NullReferenceException ex)
				{
					_logger.LogWarning(ex, "Cancellation token object disposed, null reference.");
					_cancel.Token.ThrowIfCancellationRequested();
				}
				catch (InvalidOperationException ex)
				{
					_logger.LogError(ex, "IMAP client is being accessed by multiple threads.");
					_cancel.Cancel(false);
				}
				catch (OperationCanceledException) // includes TaskCanceledException
				{
					_logger.LogTrace("Idle wait task cancelled.");
					_done?.Cancel(false);
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "IMAP idle client failed while waiting for new messages.");
					throw;
				}
			} while (_done != null &&
				!_done.IsCancellationRequested &&
				!_cancel.IsCancellationRequested);
		}

		async Task IdleAsync(CancellationToken cancellationToken = default)
        {
            if (cancellationToken != default)
                _cancel = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            do
            {
				try
				{
					if (!_messagesArrived)
						await WaitForNewMessagesAsync();
					if (_messagesArrived && !_processingMessages)
					{
						_processingMessages = true;
						_messagesArrived = false;
						await ProcessMessagesAsync();
						_processingMessages = false;
					}
				}
				catch (SocketException ex) // thrown from Reconnect() after IOException
				{
					_logger.LogWarning(ex, "Error re-thrown from Reconnect() after IOException. {0}", ex.Message);
				}
				catch (OperationCanceledException) // includes TaskCanceledException
				{
					_logger.LogDebug("Idle task cancelled.");
					break;
				}
				catch (AuthenticationException ex)
				{
					_logger.LogWarning(ex, "{0}: Stopping idle client service.", ex.GetType().Name);
					break;
				}
				catch (Exception ex)
				{
					_logger.LogWarning(ex, "Imap idle client failed while idleing or processing messages.");
					throw;
				}
			}
			while (!_cancel.IsCancellationRequested);
		}

		// Note: the CountChanged event will fire when new messages arrive in the folder and/or when messages are expunged.
		// Keep track of changes to the number of messages in the folder (this is how we'll tell if new messages have arrived).
		void OnCountChanged(object sender, EventArgs e)
		{
			if (sender is ImapFolder folder)
			{
                // Note: because we are keeping track of the MessageExpunged event and updating our
                // 'messages' list, we know that if we get a CountChanged event and folder.Count is
                // larger than messages.Count, then it means that new messages have arrived.
                int changeCount = folder.Count - _messageCache.Count;
                if (changeCount > 0)
                {
                    using (_logger.BeginScope("OnCountChanged"))
                        _logger.LogTrace("[folder] '{0}' message count increased by {1} ({2} to {3}) at {4:HH':'mm':'ss'.'fff}.",
                            folder.FullName, changeCount, _messageCache.Count, folder.Count, DateTime.Now);

                    // The ImapFolder is not re-entrant, so fetch the summaries later
                    _messagesArrived = true;
                    _done?.Cancel();
                }
            }
		}

		// Keep track of messages being expunged so that when the CountChanged event fires, we can tell if it's
		// because new messages have arrived vs messages being removed (or some combination of the two).
		void OnMessageExpunged(object sender, MessageEventArgs e)
		{
			if (e.Index < _messageCache.Count)
			{
				// remove the locally cached message at e.Index.
				_messageCache.RemoveAt(e.Index);

				if (sender is ImapFolder folder)
					_logger.LogTrace("{0}: message index {1} expunged at {2:HH':'mm':'ss'.'fff}.",
						folder.FullName, e.Index, DateTime.Now);
			}
		}

		// keep track of flag changes
		void OnMessageFlagsChanged(object sender, MessageFlagsChangedEventArgs e)
		{
			if (sender is ImapFolder folder)
				_logger.LogTrace("{0}: flags have changed for message index {1} ({2}).",
					folder.FullName, e.Index, e.Flags);
		}

		public void Cancel()
		{
            _cancel?.Cancel(false);
		}
	}
}
