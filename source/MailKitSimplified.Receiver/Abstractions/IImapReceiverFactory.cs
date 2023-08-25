using System;
using System.Collections.Generic;

namespace MailKitSimplified.Receiver.Abstractions
{
    public interface IImapReceiverFactory
    {
        IList<IImapReceiver> GetAllImapReceivers();
        IImapReceiver GetImapReceiver(string imapHost);
    }
}