using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
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
            using var smtpSender = App.ServiceProvider?.GetService<ISmtpSender>();
            if (smtpSender != null)
            {
                await smtpSender.WriteEmail
                    .From(FromTextBox)
                    .To(ToTextBox)
                    .Subject(SubjectTextBox)
                    .BodyHtml(MessageTextBox)
                    .SendAsync();
                StatusText = $"Email #{++_count} sent with subject: \"{SubjectTextBox}\".";
            }
            else
            {
                StatusText = $"Email #{++_count} sent!";
            }
        }
    }
}