using System;
using System.Text;
using System.Threading.Tasks;

namespace MailKitSimplified.Receiver.Abstractions
{
    public interface ILogFileWriter : IDisposable
    {
        void WriteLine(string textToEnqueue);

        void Write(StringBuilder textToEnqueue);

        Task<string> ReadAllTextAsync();
    }
}