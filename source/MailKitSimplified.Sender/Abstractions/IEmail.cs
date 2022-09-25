using System.Threading;
using System.Threading.Tasks;

namespace MailKitSimplified.Sender.Abstractions
{
    public interface IEmail
    {
        IEmail From(string emailAddress, string name = "");

        IEmail To(string emailAddress, string name = "");

        IEmail Subject(string subject);

        IEmail Body(string body, bool isHtml = true);

        IEmail Attach(params string[] filePath);

        Task SendAsync(CancellationToken cancellationToken = default);
    }
}
