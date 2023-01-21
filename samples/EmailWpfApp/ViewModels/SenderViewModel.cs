using System;
using System.Windows;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using EmailWpfApp.Helpers;
using EmailWpfApp.Data;
using EmailWpfApp.Models;
using Microsoft.Extensions.DependencyInjection;
using MailKitSimplified.Sender.Abstractions;

namespace EmailWpfApp.ViewModels
{
    class SenderViewModel : BaseViewModel
    {
        #region Public Properties
        public ICommand UserCommand { get; set; }

        private string _userInputText = string.Empty;
        public string UserInputTextBox
        {
            get => _userInputText;
            set
            {
                _userInputText = value;
                NotifyPropertyChanged();
            }
        }
        #endregion

        private int _count = 0;

        public SenderViewModel()
        {
            UserCommand = new RelayCommand(SendMail);
            StatusText = string.Empty;
#if DEBUG
            UserInputTextBox = "Hi.";
#endif
        }

        private void SendMail()
        {
            using var smtpSender = App.ServiceProvider?.GetService<ISmtpSender>();
            if (smtpSender != null)
            {
                smtpSender.WriteEmail
                    .To("to@localhost")
                    .Subject(UserInputTextBox)
                    .Send();
                StatusText = $"Email #{++_count} sent with message: \"{UserInputTextBox}\".";
            }
            else
            {
                StatusText = $"Email #{++_count} sent!";
                if (!string.IsNullOrWhiteSpace(StatusText))
                {
                    logger.LogDebug("Result: {0}", StatusText);
                }
            }
        }
    }
}