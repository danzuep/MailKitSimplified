using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.Extensions.DependencyInjection;
using MailKitSimplified.Receiver.Abstractions;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using EmailWpfApp.Extensions;
using EmailWpfApp.Models;
using EmailWpfApp.Data;

namespace EmailWpfApp.ViewModels
{
    public sealed partial class ReceiverViewModel : BaseViewModel, IDisposable
    {
        public ObservableCollection<string> ViewModelItems { get; private set; } = new() { _inbox };
        public string SelectedViewModelItem { get; set; } = _inbox;

        public ObservableCollection<Email> ViewModelDataGrid { get; private set; } = new();

        [ObservableProperty]
        private string _messageTextBlock = string.Empty;

        private int _count = 0;
        private static readonly string _inbox = "INBOX";
        private readonly IMailFolderReader? _mailFolderReader;
        private readonly EmailDbContext? _dbContext;

        public ReceiverViewModel() : base()
        {
            _mailFolderReader = App.ServiceProvider?.GetService<IMailFolderReader>();
            _dbContext = App.ServiceProvider?.GetService<EmailDbContext>();
            StatusText = string.Empty;
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            GetFoldersAsync();
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        }

        private async Task GetFoldersAsync()
        {
            try
            {
                if (App.ServiceProvider?.GetService<IImapReceiver>() is IImapReceiver imapReceiver)
                {
                    StatusText = "Getting mail folder names...";
                    var mailFolderNames = await imapReceiver.GetMailFolderNamesAsync();
                    if (mailFolderNames.Count > 0)
                        ViewModelItems = new ObservableCollection<string>(mailFolderNames);
                    StatusText = string.Empty;
                }
                else if (_dbContext != null)
                {
                    var emails = _dbContext.Emails.AsEnumerable();
                    var collection = new ObservableCollection<Email>(emails);
                    ViewModelDataGrid = collection;
                }
            }
            catch (Exception ex)
            {
                ShowAndLogError(ex);
                System.Diagnostics.Debugger.Break();
            }
        }

        private void StoreEmails(IEnumerable<Email> emails)
        {
            try
            {
                _dbContext?.Emails.UpdateRange(emails);
            }
            catch (Exception ex)
            {
                ShowAndLogWarning(ex);
            }
        }

        [RelayCommand]
        private async Task ReceiveMailAsync()
        {
            if (_mailFolderReader != null)
            {
                StatusText = "Getting email...";
                var mimeMessages = await _mailFolderReader
                    .Take(1, continuous: true).GetMimeMessagesAsync();
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
            else
            {
                StatusText = $"Email #{++_count} received!";
            }
        }

        public void Dispose()
        {
            _mailFolderReader?.Dispose();
            _dbContext?.Dispose();
        }
    }
}
