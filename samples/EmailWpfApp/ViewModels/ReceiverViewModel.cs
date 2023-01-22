using System.Linq;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using MailKitSimplified.Receiver.Services;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using MailKit.Search;
using MailKitSimplified.Receiver.Abstractions;
using EmailWpfApp.Models;
using MailKit;

namespace EmailWpfApp.ViewModels
{
    class ReceiverViewModel : BaseViewModel
    {
        #region Public Properties
        public ObservableCollection<string> ViewModelData { get; private set; } = new();

        public IAsyncRelayCommand ReceiveCommand { get; set; }

        private string _messageText = string.Empty;
        public string MessageTextBlock
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

        public ReceiverViewModel() : base()
        {
            ReceiveCommand = new AsyncRelayCommand(ReceiveMailAsync);
            StatusText = string.Empty;
        }

        private async Task ReceiveMailAsync()
        {
            using var imapReceiver = App.ServiceProvider?.GetService<IImapReceiver>();
            if (imapReceiver != null)
            {
                var mimeMessages = await imapReceiver.ReadMail
                    .Take(1).GetMimeMessagesAsync();
                var m = mimeMessages.Single();
                var email = Email.Write
                    .From(m.From.ToString())
                    .To(m.To.ToString())
                    .Subject(m.Subject)
                    .BodyHtml(m.Body)
                    .AsEmail;
                ViewModelData.Add(email.ToString());
                StatusText = $"Email received: {email.Subject}.";
                MessageTextBlock = email.ToString();
            }
            else
            {
                ViewModelData.Add($"Email #{++_count}");
                StatusText = $"Email #{_count} received!";
            }
        }
    }
}
