using System.Threading;
using System.Threading.Tasks;
using MimeKit;

namespace MailKitSimplified.Sender.Abstractions
{
    public interface IMimeMessageWriter
    {
        IMimeMessageWriter From(string emailAddress, string name = "");

        IMimeMessageWriter To(string emailAddress, string name = "");

        IMimeMessageWriter Cc(string emailAddress, string name = "");

        IMimeMessageWriter Bcc(string emailAddress, string name = "");

        IMimeMessageWriter Subject(string subject, bool append = false);

        IMimeMessageWriter Body(string body, bool isHtml = true);

        IMimeMessageWriter Attach(params string[] filePath);

        MimeMessage MimeMessage { get; }

        Task SendAsync(CancellationToken cancellationToken = default);

        Task<bool> TrySendAsync(CancellationToken cancellationToken = default);
    }
}
