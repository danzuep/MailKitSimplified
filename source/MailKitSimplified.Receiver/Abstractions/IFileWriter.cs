using System.Threading.Tasks;

namespace MailKitSimplified.Receiver.Abstractions
{
    public interface IFileWriter
    {
        Task WriteLineAsync(string textToWrite);
    }
}