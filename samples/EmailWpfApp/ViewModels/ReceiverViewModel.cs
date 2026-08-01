using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using EmailWpfApp.Extensions;
using EmailWpfApp.Models;
using MailKitSimplified.Receiver.Abstractions;

namespace EmailWpfApp.ViewModels
{
    public sealed partial class ReceiverViewModel : BaseViewModel, IDisposable
    {
        public ObservableCollection<string> ViewModelItems { get; private set; } = new() { _inbox };
        public string SelectedViewModelItem { get; set; } = _inbox;

        public ObservableCollection<Email> ViewModelDataGrid { get; private set; } = new();

        [ObservableProperty]
        private string _messageTextBlock = string.Empty;

        [ObservableProperty]
        private bool isInProgress;

        private readonly Lazy<Task> GetFoldersTask;
        private int _count = 0;
        private static readonly string _inbox = "INBOX";
        private readonly IMailFolderReader _mailFolderReader;

        public ReceiverViewModel() : base()
        {
            _mailFolderReader = Ioc.Default.GetRequiredService<IMailFolderReader>();
            GetFoldersTask = new Lazy<Task>(GetFoldersAsync);
            StatusText = string.Empty;
        }

        private async Task GetFoldersAsync()
        {
            IsInProgress = true;
            if (Ioc.Default.GetRequiredService<IImapReceiver>() is IImapReceiver imapReceiver)
            {
                UpdateStatusText("Getting mail folder names...");
                var mailFolderNames = await imapReceiver.GetMailFolderNamesAsync();
                if (mailFolderNames.Count > 0)
                    ViewModelItems = new ObservableCollection<string>(mailFolderNames);
                UpdateStatusText(string.Empty);
            }
            IsInProgress = false;
        }

        private void StoreEmails(IEnumerable<Email> emails)
        {
            // TODO
        }

        private readonly SemaphoreSlim _imapSemaphore = new SemaphoreSlim(1, 1);

        private async Task<T> WithImapAsync<T>(Func<Task<T>> action)
        {
            await _imapSemaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                await GetFoldersTask.Value.ConfigureAwait(false);
                return await action().ConfigureAwait(false);
            }
            finally
            {
                _imapSemaphore.Release();
            }
        }

        [RelayCommand]
        private async Task ReceiveMailAsync()
        {
            try
            {
                StatusText = "Getting email...";
                IsInProgress = true;
                var mimeMessages = await WithImapAsync(() =>
                    _mailFolderReader.Take(1, continuous: true).GetMimeMessagesAsync());
                var emails = mimeMessages.Convert();
                var count = 0;
                foreach (var email in emails)
                {
                    StatusText = $"Email #{++_count} received: {email.Subject}.";
                    MessageTextBlock = email.ToString();
                    ViewModelDataGrid.Add(email);
                    count++;
                }
                if (count > 0)
                    StoreEmails(ViewModelDataGrid.AsEnumerable());
                else
                    StatusText = "No more emails in this folder.";
            }
            catch (Exception ex)
            {
                ShowAndLogError(ex);
                System.Diagnostics.Debugger.Break();
            }
            IsInProgress = false;
        }

        public void Dispose()
        {
            _mailFolderReader?.Dispose();
            //_dbContext?.Dispose();
        }
    }
}