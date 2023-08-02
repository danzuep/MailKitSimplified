using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using MailKitSimplified.Sender.Abstractions;
using EmailWpfApp.Models;
using MailKitSimplified.Receiver.Abstractions;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System;
using System.Windows.Controls;

namespace EmailWpfApp.ViewModels
{
    public sealed partial class SenderViewModel : BaseViewModel
    {
        [ObservableProperty]
        private string _fromTextBox = string.Empty;

        [ObservableProperty]
        private string _toTextBox = string.Empty;

        [ObservableProperty]
        private string _subjectTextBox = string.Empty;

        [ObservableProperty]
        private string _messageTextBox = string.Empty;

        [ObservableProperty]
        private bool isInProgress;

        private int _count = 0;

        public SenderViewModel() : base()
        {
            StatusText = string.Empty;
#if DEBUG
            FromTextBox = "from@localhost";
            ToTextBox = "to@localhost";
            MessageTextBox = "<p>Hi.<p>";
            SubjectTextBox = "1";
#endif
        }

        [RelayCommand]
        private async Task SendMailAsync()
        {
            IsInProgress = true;
            try
            {
                using var smtpSender = Ioc.Default.GetRequiredService<ISmtpSender>();
                if (smtpSender != null)
                {
                    await smtpSender.WriteEmail
                        .DefaultFrom(FromTextBox)
                        .From(FromTextBox)
                        .To(ToTextBox)
                        .Subject(SubjectTextBox)
                        .BodyHtml(MessageTextBox)
                        .SendAsync();
                    StatusText = $"Email #{++_count} sent with subject: \"{SubjectTextBox}\".";
#if DEBUG
                    SubjectTextBox = (_count + 1).ToString();
#endif
                }
            }
            catch (Exception ex)
            {
                ShowAndLogError(ex);
                System.Diagnostics.Debugger.Break();
            }
            IsInProgress = false;
        }
    }
}