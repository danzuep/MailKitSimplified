using System.Threading.Tasks;
using MailKitSimplified.Sender.Abstractions;
using MailKitSimplified.Receiver.Abstractions;

namespace EmailWpfApp.State.Authenticators
{
    public interface IAuthenticator
    {
        bool IsLoggedIn { get; }
        Task Login(ISmtpSender? smtpSender = null);
        Task Login(IImapReceiver? imapReceiver = null);
    }
}
