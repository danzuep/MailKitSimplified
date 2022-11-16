using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using MimeKit;
using System.IO;
using MailKit;

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

        IEmailWriter Attach(params string[] filePaths);

        IEmailWriter TryAttach(params string[] filePaths);

        IEmailWriter Attach(Stream stream, string fileName, string contentType = "", string contentId = "", bool linkedResource = false);

        IEmailWriter Attach(MimeEntity mimeEntity, bool linkedResource = false);

        IEmailWriter Attach(IEnumerable<MimeEntity> mimeEntities, bool linkedResource = false);

        MimeMessage MimeMessage { get; }

        void Send(CancellationToken cancellationToken = default, ITransferProgress transferProgress = null);

        bool TrySend(CancellationToken cancellationToken = default, ITransferProgress transferProgress = null);

        Task SendAsync(CancellationToken cancellationToken = default, ITransferProgress transferProgress = null);

        Task<bool> TrySendAsync(CancellationToken cancellationToken = default, ITransferProgress transferProgress = null);
    }
}
