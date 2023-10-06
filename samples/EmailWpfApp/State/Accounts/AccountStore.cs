using MailKitSimplified.Receiver.Models;
using MailKitSimplified.Sender.Models;
using System;

namespace EmailWpfApp.State.Accounts
{
    public class AccountStore : IAccountStore
    {
        private EmailSenderOptions? _currentSmtpAccount;
        public EmailSenderOptions? CurrentSmtpAccount
        {
            protected get => _currentSmtpAccount;
            set
            {
                _currentSmtpAccount = value;
                StateChanged?.Invoke();
            }
        }

        private EmailReceiverOptions? _currentImapAccount;
        public EmailReceiverOptions? CurrentImapAccount
        {
            protected get => _currentImapAccount;
            set
            {
                _currentImapAccount = value;
                StateChanged?.Invoke();
            }
        }

        public event Action? StateChanged;
    }
}
