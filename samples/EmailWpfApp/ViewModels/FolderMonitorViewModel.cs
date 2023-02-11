using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using MailKitSimplified.Receiver.Abstractions;
using MailKitSimplified.Receiver.Extensions;
using EmailWpfApp.Extensions;
using EmailWpfApp.Models;
using System.Windows.Threading;
using MailKitSimplified.Sender.Models;
using System.Linq;
using System.ComponentModel;
using System.Windows;
using System.Threading;
using System.Windows.Controls;
using MailKit;

namespace EmailWpfApp.ViewModels
{
    public sealed partial class FolderMonitorViewModel : BaseViewModel, IDisposable
    {
        public ObservableCollection<string> ViewModelItems { get; private set; } = new() { _inbox };
        public string SelectedViewModelItem { get; set; } = _inbox;

        public ObservableCollection<Email> ViewModelDataGrid { get; private set; } = new();
        private List<Email> emails = new();
        public Email? SelectedEmail { get; set; }

        [ObservableProperty]
        private string imapHost = "localhost";

        [ObservableProperty]
        private bool isInProgress;

        [ObservableProperty]
        private int progressBarPercentage;

        [ObservableProperty]
        private string _messageTextBlock = string.Empty;

        public IProgress<Email> ProgressEmail;

        private readonly BackgroundWorker worker = new BackgroundWorker();
        private static readonly string _inbox = "INBOX";
        private readonly IImapReceiver _imapReceiver;

        public FolderMonitorViewModel() : base()
        {
            _imapReceiver = Ioc.Default.GetRequiredService<IImapReceiver>();
            ProgressEmail = new Progress<Email>(UpdateProgressEmail);
            StatusText = string.Empty;
        }

        internal void UpdateProgressEmail(Email email)
        {
            //ViewModelDataGrid.Add(email);
            emails.Add(email);
        }

        [RelayCommand]
        private async Task ConnectHostAsync()
        {
            try
            {
                StatusText = "Getting mail folder names...";
                IsInProgress = true;
                var mailFolderNames = await _imapReceiver.GetMailFolderNamesAsync();
                if (mailFolderNames.Count > 0)
                {
                    ViewModelItems = new ObservableCollection<string>(mailFolderNames);
                    //StoreFolderNames(mailFolderNames);
                }
                IsInProgress = false;
                StatusText = $"Connected to {ImapHost}.";
            }
            catch (Exception ex)
            {
                ShowAndLogError(ex);
                System.Diagnostics.Debugger.Break();
            }
        }

        //[RelayCommand]
        //private async Task ChangeFolderAsync()
        //{
        //    //await _imapReceiver.ConnectMailFolderAsync(SelectedViewModelItem);
        //    var idleTask = Task.Run(() => _imapReceiver.MonitorFolder
        //        .OnMessageArrival(OnArrivalAsync)
        //        .IdleAsync());
        //    //Dispatcher.InvokeAsync();
        //    await Task.Delay(2000);
        //    ViewModelDataGrid = new ObservableCollection<Email>(emails);
        //    if (SelectedEmail == null)
        //    {
        //        SelectedEmail = emails.FirstOrDefault();
        //    }
        //    OnPropertyChanged(nameof(ViewModelDataGrid));
        //    //await idleTask;
        //}

        CancellationTokenSource cts = new CancellationTokenSource();

        [RelayCommand]
        private void Receive()
        {
            if (ProgressBarPercentage > 0)
            {
                cts.Cancel(false);
                worker.DoWork -= BackgroundWorkerDoWork;
                worker.ProgressChanged -= BackgroundWorkerProgressChanged;
                worker.RunWorkerCompleted -= BackgroundWorkerRunWorkerCompleted;
                worker.Dispose();
                ProgressBarPercentage = 0;
                return;
            }
            ProgressBarPercentage = 1;
            worker.WorkerSupportsCancellation = true;
            worker.WorkerReportsProgress = true;
            worker.DoWork += BackgroundWorkerDoWork;
            worker.ProgressChanged += BackgroundWorkerProgressChanged;
            worker.RunWorkerCompleted += BackgroundWorkerRunWorkerCompleted;
            worker.RunWorkerAsync();
        }

        [RelayCommand]
        private void Cancel()
        {
            worker?.CancelAsync();
        }

        private async Task OnArrivalAsync(MailKit.IMessageSummary messageSummary)
        {
            UpdateStatusText("Downloading email...");
            IsInProgress = true;
            var mimeMessage = await messageSummary.GetMimeMessageAsync();
            var email = mimeMessage.Convert();
            UpdateStatusText($"{_imapReceiver} #{messageSummary.Index} received: {email.Subject}.");
            ViewModelDataGrid.Add(email);
            if (SelectedEmail == null)
            {
                SelectedEmail = email;
            }
            IsInProgress = false;
            //UpdateStatusText(string.Empty);
        }

        private void BackgroundWorkerDoWork(object? sender, DoWorkEventArgs e)
        {
            if (sender is BackgroundWorker worker)
            {
                if (worker.CancellationPending)
                {
                    e.Cancel = true;
                    return;
                }
                if (e.Argument is int max)
                {
                    //int progressPercentage = Convert.ToInt32((max * 100d) / 100);
                }
                var idleTask = _imapReceiver.MonitorFolder
                    .OnMessageArrival(OnMessageArrived)
                    .IdleAsync(cts.Token);
                idleTask.Wait();
                e.Result = 100;
            }
        }

        private void OnMessageArrived(IMessageSummary messageSummary)
        {
            int progressPercentage = ProgressBarPercentage + 10;
            worker.ReportProgress(progressPercentage, messageSummary);
            Thread.Sleep(1);
        }

        private void BackgroundWorkerProgressChanged(object? sender, ProgressChangedEventArgs e)
        {
            ProgressBarPercentage = e.ProgressPercentage;
            if (e.UserState is IMessageSummary messageSummary)
            {
                _ = OnArrivalAsync(messageSummary);
            }
        }

        private void BackgroundWorkerRunWorkerCompleted(object? sender, RunWorkerCompletedEventArgs e)
        {
            ProgressBarPercentage = Convert.ToInt32(e.Result);
        }

        //private void StoreFolderNames(IEnumerable<string> folderNames)
        //{
        //    try
        //    {
        //        Guard.IsNotNull(folderNames, nameof(folderNames));
        //        if (Ioc.Default.GetService<EmailDbContext>() is EmailDbContext dbContext)
        //        {
        //            dbContext.Folders.UpdateRange(folderNames);
        //            dbContext.Dispose();
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        ShowAndLogWarning(ex);
        //    }
        //}

        public void Dispose()
        {
            _imapReceiver.Dispose();
        }
    }
}
