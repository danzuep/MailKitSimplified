using System;

namespace MailKitSimplified.Receiver.Abstractions
{
    public interface IMailFolderReader : IMailReader, IAsyncDisposable, IDisposable
    {
    }
}
