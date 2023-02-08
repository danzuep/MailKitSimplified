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
        private string _messageTextBlock = string.Empty;

        [ObservableProperty]
        private bool isInProgress;

        [ObservableProperty]
        private string imapHost = "localhost";

        public IProgress<Email> ProgressEmail;

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
                StatusText = string.Empty;
            }
            catch (Exception ex)
            {
                ShowAndLogError(ex);
                System.Diagnostics.Debugger.Break();
            }
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

        [RelayCommand]
        private async Task ChangeFolderAsync()
        {
            //await _imapReceiver.ConnectMailFolderAsync(SelectedViewModelItem);
            var idleTask = Task.Run(() => _imapReceiver.MonitorFolder
                .OnMessageArrival(OnArrivalAsync)
                .IdleAsync());
            //Dispatcher.InvokeAsync();
            await Task.Delay(2000);
            ViewModelDataGrid = new ObservableCollection<Email>(emails);
            if (SelectedEmail == null)
            {
                SelectedEmail = emails.FirstOrDefault();
            }
            OnPropertyChanged(nameof(ViewModelDataGrid));
            //await idleTask;
        }

        private async Task OnArrivalAsync(MailKit.IMessageSummary messageSummary)
        {
            UpdateStatusText("Downloading email...");
            IsInProgress = true;
            var mimeMessage = await messageSummary.GetMimeMessageAsync();
            var email = mimeMessage.Convert();
            UpdateStatusText($"{_imapReceiver} #{messageSummary.Index} received: {email.Subject}.");
            UpdateProgressEmail(email);
            //ViewModelDataGrid.Add(email);
            IsInProgress = false;
            //UpdateStatusText(string.Empty);
        }

        public void Dispose()
        {
            _imapReceiver.Dispose();
        }
    }
}
