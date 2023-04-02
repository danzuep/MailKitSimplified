using System;
using System.Collections.Generic;

namespace MailKitSimplified.Receiver.Abstractions
{
    public interface IImapReceiverFactory : IDisposable
    {
        IList<IImapReceiver> GetAllImapReceivers();
        IImapReceiver GetImapReceiver(string imapHost);
    }
}