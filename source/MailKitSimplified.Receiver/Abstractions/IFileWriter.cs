using System;

namespace MailKitSimplified.Receiver.Abstractions
{
    public interface IFileWriter : IDisposable
    {
        void Write(string textToEnqueue);
    }
}