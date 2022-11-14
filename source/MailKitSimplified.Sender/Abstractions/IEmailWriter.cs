using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using MimeKit;

namespace MailKitSimplified.Sender.Abstractions
{
    public interface IEmailWriter
    {
        IEmailWriter From(string name, string emailAddress, bool replyTo = true);

        IEmailWriter From(string emailAddress, bool replyTo = true);

        IEmailWriter To(string name, string emailAddress);

        IEmailWriter To(string emailAddress);

        IEmailWriter Cc(string name, string emailAddress);

        IEmailWriter Cc(string emailAddress);

        IEmailWriter Bcc(string name, string emailAddress);

        IEmailWriter Bcc(string emailAddress);

        IEmailWriter Subject(string subject, bool append = false);

        IEmailWriter BodyHtml(string textHtml);

        IEmailWriter BodyText(string textPlain);

        IEmailWriter Attach(MimeEntity mimeEntity, bool resource = false);

        IEmailWriter Attach(IEnumerable<MimeEntity> mimeEntities, bool resource = false);

        IEmailWriter Attach(params string[] filePaths);

        IEmailWriter TryAttach(params string[] filePaths);

        MimeMessage MimeMessage { get; }

        void Send(CancellationToken cancellationToken = default);

        bool TrySend(CancellationToken cancellationToken = default);

        Task SendAsync(CancellationToken cancellationToken = default);

        Task<bool> TrySendAsync(CancellationToken cancellationToken = default);
    }
}
