using MailKitSimplified.Receiver.Models;
using MailKitSimplified.Sender.Models;
using System;

namespace EmailWpfApp.State.Accounts
{
    public interface IAccountStore
    {
        EmailSenderOptions? CurrentSmtpAccount { set; }
        EmailReceiverOptions? CurrentImapAccount { set; }

        event Action StateChanged;
    }
}
