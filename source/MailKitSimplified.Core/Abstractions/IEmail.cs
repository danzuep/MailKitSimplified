using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace MailKitSimplified.Core.Abstractions
{
    [Obsolete("Use ISendableEmail instead.")]
    public interface IEmail
    {
        IList<IEmailAddress> From { get; set; }
        IList<IEmailAddress> To { get; set; }
        IList<string> AttachmentFilePaths { get; set; }
        string Subject { get; set; }
        string Body { get; set; }
        bool IsHtml { get; set; }
        Task SendAsync(ISendableEmail email, CancellationToken cancellationToken = default);
        Task<bool> TrySendAsync(ISendableEmail email, CancellationToken cancellationToken = default);
    }
}
