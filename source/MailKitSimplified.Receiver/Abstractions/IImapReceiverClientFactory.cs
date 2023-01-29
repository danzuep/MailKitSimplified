using System;

namespace MailKitSimplified.Receiver.Abstractions
{
    public interface IImapReceiverClientFactory : IDisposable
    {
        IImapReceiver GetClient(string clientId);
    }
}