using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using MimeKit;

namespace MailKitSimplified.Sender.Abstractions
{
    public interface IEmailWriter
    {
        IEmailWriter From(string emailAddress, string name = "");

        IEmailWriter To(string emailAddress, string name = "");

        IEmailWriter Cc(string emailAddress, string name = "");

        IEmailWriter Bcc(string emailAddress, string name = "");

        IEmailWriter Subject(string subject, bool append = false);

        IEmailWriter Body(string body, bool isHtml = true);

        IEmailWriter Attach(params string[] filePaths);

        IEmailWriter Attach(MimePart mimePart, bool resource = false);

        IEmailWriter Attach(IEnumerable<MimePart> mimeParts, bool resource = false);

        MimeMessage MimeMessage { get; }

        Task SendAsync(CancellationToken cancellationToken = default);

        Task<bool> TrySendAsync(CancellationToken cancellationToken = default);
    }
}
