using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using MailKitSimplified.Sender.Abstractions;
using System.Threading.Tasks;

namespace EmailWpfApp.ViewModels
{
    class SenderViewModel : BaseViewModel
    {
        #region Public Properties
        public IAsyncRelayCommand SendCommand { get; set; }

        private string _fromText = string.Empty;
        public string FromTextBox
        {
            get => _fromText;
            set
            {
                _fromText = value;
                OnPropertyChanged();
            }
        }

        private string _toText = string.Empty;
        public string ToTextBox
        {
            get => _toText;
            set
            {
                _toText = value;
                OnPropertyChanged();
            }
        }

        private string _subjectText = string.Empty;
        public string SubjectTextBox
        {
            get => _subjectText;
            set
            {
                _subjectText = value;
                OnPropertyChanged();
            }
        }

        private string _messageText = string.Empty;
        public string MessageTextBox
        {
            get => _messageText;
            set
            {
                _messageText = value;
                OnPropertyChanged();
            }
        }
        #endregion

        private int _count = 0;

        public SenderViewModel() : base()
        {
            SendCommand = new AsyncRelayCommand(SendMailAsync);
            StatusText = string.Empty;
#if DEBUG
            FromTextBox = "from@localhost";
            ToTextBox = "to@localhost";
            SubjectTextBox = "Hey";
            MessageTextBox = "<p>Hi.<p>";
#endif
        }

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
                StatusText = $"Email #{++_count} sent with message: \"{SubjectTextBox}\".";
            }
            else
            {
                StatusText = $"Email #{++_count} sent!";
            }
        }
    }
}