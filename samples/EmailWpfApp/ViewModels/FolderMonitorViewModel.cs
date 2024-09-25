﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using EmailWpfApp.Extensions;
using EmailWpfApp.Models;
using MailKit;
using MailKitSimplified.Receiver.Abstractions;
using MailKitSimplified.Receiver.Extensions;
using MailKitSimplified.Receiver.Services;
using Microsoft.Extensions.Logging;

namespace EmailWpfApp.ViewModels
{
    public sealed partial class FolderMonitorViewModel : BaseViewModel, IDisposable
    {
        private readonly Channel<IMessageSummary> _queue;

        public ObservableCollection<string> ViewModelItems { get; private set; } = new() { _inbox };
        public string SelectedViewModelItem { get; set; } = _inbox;

        public ObservableCollection<Email> ViewModelDataGrid { get; private set; } = new();

        [ObservableProperty]
        private Email selectedEmail = new();

        [ObservableProperty]
        private string imapHost = "localhost:143";

        [ObservableProperty]
        private string username = string.Empty;

        [ObservableProperty]
        private string password = string.Empty;

        [ObservableProperty]
        private bool isNotReceiving = true;

        [ObservableProperty]
        private bool isReceiving;

        [ObservableProperty]
        private bool isInProgress;

        [ObservableProperty]
        private int progressBarPercentage;

        [ObservableProperty]
        private string _messageTextBlock = string.Empty;

        private static readonly string _inbox = "INBOX";
        private CancellationTokenSource _cts = new();
        private readonly BackgroundWorker _worker = new();
        //private readonly EmailDbContext? _dbContext;
        private IImapReceiver _imapReceiver;
        private readonly ILogger _logger;

        public FolderMonitorViewModel() : base()
        {
            _imapReceiver = Ioc.Default.GetRequiredService<IImapReceiver>();
            _logger = Ioc.Default.GetRequiredService<ILogger<FolderMonitorViewModel>>();
            //_dbContext = Ioc.Default.GetService<EmailDbContext>();
            StatusText = string.Empty;

            int capacity = 0;
            if (capacity > 0)
            {
                var channelOptions = new BoundedChannelOptions(capacity)
                {
                    FullMode = BoundedChannelFullMode.Wait,
                    AllowSynchronousContinuations = true,
                    SingleReader = true,
                    SingleWriter = true
                };
                _queue = Channel.CreateBounded<IMessageSummary>(channelOptions);
            }
            else
            {
                var channelOptions = new UnboundedChannelOptions()
                {
                    AllowSynchronousContinuations = true,
                    SingleReader = true,
                    SingleWriter = true
                };
                _queue = Channel.CreateUnbounded<IMessageSummary>(channelOptions);
            }
        }

        private async Task GetMailFolderNamesAsync(CancellationToken cancellationToken = default)
        {
            var mailFolderNames = await _imapReceiver.GetMailFolderNamesAsync(cancellationToken);
            if (mailFolderNames.Count > 0)
            {
                ViewModelItems = new ObservableCollection<string>(mailFolderNames);
                StoreFolderNames(mailFolderNames);
            }
        }

        private void StoreFolderNames(IEnumerable<string> folderNames)
        {
            try
            {
                Guard.IsNotNull(folderNames, nameof(folderNames));
                //_dbContext?.Folders.UpdateRange(folderNames);
            }
            catch (Exception ex)
            {
                ShowAndLogWarning(ex);
            }
        }

        [RelayCommand]
        private async Task ConnectHostAsync()
        {
            if (!IsNotReceiving)
            {
                _cts.Cancel();
                _cts.TryReset();
                _cts = new();
                IsReceiving = false;
                IsNotReceiving = !IsReceiving;
                return;
            }
            IsInProgress = true;
            try
            {
#if DEBUG
                var cred = new NetworkCredential(Username, Password);
                _imapReceiver = ImapReceiver.Create(ImapHost, cred);
#endif
                StatusText = "Getting mail folder names...";
                await GetMailFolderNamesAsync(_cts.Token).ConfigureAwait(false);
                StatusText = $"Connected to {ImapHost}.";
                IsReceiving = !IsNotReceiving;
            }
            catch (OperationCanceledException ex)
            {
                UpdateStatusText(ex);
            }
            catch (Exception ex)
            {
                ShowAndLogError(ex);
                System.Diagnostics.Debugger.Break();
            }
            IsInProgress = false;
        }

        [RelayCommand]
        private void Receive()
        {
            Task.Run(async () =>
            {
                IsNotReceiving = false;
                try
                {
                    if (!IsReceiving)
                        await GetMailFolderNamesAsync().ConfigureAwait(false);
                    IsReceiving = true;
                    var tasks = new Task[]
                    {
                    _imapReceiver.MonitorFolder.OnMessageArrival(EnqueueAsync).IdleAsync(_cts.Token),
                    ProcessQueueAsync(OnArrivalAsync, _cts.Token)
                    };
                    await Task.WhenAll(tasks);
                }
                catch (OperationCanceledException ex)
                {
                    UpdateStatusText(ex);
                }
                catch (Exception ex)
                {
                    ShowAndLogError(ex);
                }
                IsReceiving = false;
                IsNotReceiving = true;
            });
        }

        private async Task EnqueueAsync(IMessageSummary m) => await _queue.Writer.WriteAsync(m, _cts.Token);

        private async Task ProcessQueueAsync(Func<IMessageSummary, ValueTask> messageArrivalMethod, CancellationToken cancellationToken = default)
        {
            IMessageSummary? messageItem = null;
            try
            {
                await foreach (var messageSummary in _queue.Reader.ReadAllAsync(cancellationToken))
                {
                    if (messageSummary != null)
                    {
                        messageItem = messageSummary;
                        await messageArrivalMethod(messageSummary);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogTrace("Arrival queue cancelled.");
            }
            catch (ChannelClosedException ex)
            {
                _logger.LogWarning(ex, "Channel closed.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred processing task queue item #{0}.", messageItem);
                if (messageItem != null)
                    await _queue.Writer.WriteAsync(messageItem);
            }
        }

        private async ValueTask OnArrivalAsync(IMessageSummary messageSummary)
        {
            UpdateStatusText("Downloading email...");
            IsInProgress = true;
            try
            {
                var mimeMessage = await messageSummary.GetMimeMessageAsync(_cts.Token);
                var email = mimeMessage.Convert();
                UpdateStatusText($"{_imapReceiver} #{messageSummary.Index} received: {email.Subject}.");
                await App.Current.Dispatcher.InvokeAsync(() => ViewModelDataGrid.Add(email));
                if (SelectedEmail.MailboxIndex == 0 && string.IsNullOrEmpty(SelectedEmail.MessageId))
                {
                    SelectedEmail = email;
                }
            }
            catch (Exception ex)
            {
                UpdateStatusText(ex);
            }
            IsInProgress = false;
            //UpdateStatusText(string.Empty);
        }

        public void Dispose()
        {
            //_queue.Writer.Complete();
            _imapReceiver.Dispose();
            //_dbContext?.Dispose();
            _worker.Dispose();
            _cts.Dispose();
        }
    }
}
