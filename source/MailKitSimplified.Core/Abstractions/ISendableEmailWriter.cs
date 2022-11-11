using System.Threading;
using System.Threading.Tasks;

namespace MailKitSimplified.Core.Abstractions
{
    public interface ISendableEmailWriter
    {
        ISendableEmailWriter From(string emailAddress, string name = "");

        ISendableEmailWriter To(string emailAddress, string name = "");

        ISendableEmailWriter Cc(string emailAddress, string name = "");

        ISendableEmailWriter Bcc(string emailAddress, string name = "");

        ISendableEmailWriter Subject(string subject);

        ISendableEmailWriter Body(string body, bool isHtml = true);

        ISendableEmailWriter Attach(params string[] filePath);

        ISendableEmail Result { get; }

        Task SendAsync(CancellationToken cancellationToken = default);

        Task<bool> TrySendAsync(CancellationToken cancellationToken = default);
    }
}
