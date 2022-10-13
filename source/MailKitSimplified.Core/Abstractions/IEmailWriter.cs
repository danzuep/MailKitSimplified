using System.Threading;
using System.Threading.Tasks;

namespace MailKitSimplified.Core.Abstractions
{
    public interface IEmailWriter
    {
        IEmailWriter From(string emailAddress, string name = "");

        IEmailWriter To(string emailAddress, string name = "");

        IEmailWriter Subject(string subject);

        IEmailWriter Body(string body, bool isHtml = true);

        IEmailWriter Attach(params string[] filePath);

        IEmail GetEmail { get; }

        Task SendAsync(CancellationToken cancellationToken = default);

        Task<bool> TrySendAsync(CancellationToken cancellationToken = default);
    }
}
