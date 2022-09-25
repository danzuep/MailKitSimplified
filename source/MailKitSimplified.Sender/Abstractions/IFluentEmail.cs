using System.Threading;
using System.Threading.Tasks;

namespace MailKitSimplified.Sender.Abstractions
{
    public interface IFluentEmail
    {
        IFluentEmail From(string emailAddress, string name = "");

        IFluentEmail To(string emailAddress, string name = "");

        IFluentEmail Subject(string subject);

        IFluentEmail Body(string body, bool isHtml = true);

        IFluentEmail Attach(params string[] filePath);

        Task SendAsync(CancellationToken cancellationToken = default);
    }
}
