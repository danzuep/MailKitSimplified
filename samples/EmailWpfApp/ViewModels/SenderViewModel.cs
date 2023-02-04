using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using MailKitSimplified.Sender.Abstractions;

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
            SubjectTextBox = "Hey";
            MessageTextBox = "<p>Hi.<p>";
#endif
        }

        [RelayCommand]
        private async Task SendMailAsync()
        {
            using var smtpSender = Ioc.Default.GetRequiredService<ISmtpSender>();
            if (smtpSender != null)
            {
                IsInProgress = true;
                await smtpSender.WriteEmail
                    .From(FromTextBox)
                    .To(ToTextBox)
                    .Subject(SubjectTextBox)
                    .BodyHtml(MessageTextBox)
                    .SendAsync();
                StatusText = $"Email #{++_count} sent with subject: \"{SubjectTextBox}\".";
                IsInProgress = false;
            }
            else
            {
                StatusText = $"Email #{++_count} sent!";
            }
        }
    }
}