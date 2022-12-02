using MailKit;
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MailKitSimplified.Receiver.Abstractions;

namespace MailKitSimplified.Receiver.Services
{
    public sealed class MailFolderClient : IMailFolderClient
    {
        public string MailFolderName => _mailFolder?.FullName ?? _imapReceiver.ToString();
        public int MailFolderCount => _mailFolder?.Count ?? 0;

        private IMailFolder _mailFolder = null;
        private readonly ILogger _logger;
        private readonly IImapReceiver _imapReceiver;

        public MailFolderClient(IImapReceiver imapReceiver, ILogger<MailFolderClient> logger = null)
        {
            _logger = logger ?? NullLogger<MailFolderClient>.Instance;
            _imapReceiver = imapReceiver ?? throw new ArgumentNullException(nameof(imapReceiver));
        }

        public async ValueTask<IMailFolder> ConnectAsync(bool enableWrite = false, CancellationToken cancellationToken = default)
        {
            _mailFolder = await _imapReceiver.ConnectMailFolderAsync(cancellationToken).ConfigureAwait(false);
            if (!_mailFolder.IsOpen)
            {
                var folderAccess = enableWrite ? FolderAccess.ReadWrite : FolderAccess.ReadOnly;
                _ = await _mailFolder.OpenAsync(folderAccess, cancellationToken).ConfigureAwait(false);
                _logger.LogTrace($"{this} mail folder opened with {folderAccess} access.");
            }
            else if (enableWrite && _mailFolder.Access != FolderAccess.ReadWrite)
            {
                _logger.LogTrace($"{this} mail folder SyncRoot changed for ReadWrite access.");
                await _mailFolder.OpenAsync(FolderAccess.ReadWrite, cancellationToken).ConfigureAwait(false);
            }
            return _mailFolder;
        }

        public IMailFolderClient Copy() => MemberwiseClone() as IMailFolderClient;

        public override string ToString() => $"{MailFolderName} ({MailFolderCount})";

        public async ValueTask DisposeAsync()
        {
            if (_mailFolder?.IsOpen ?? false)
                await _mailFolder.CloseAsync(false, CancellationToken.None).ConfigureAwait(false);
        }

        public void Dispose()
        {
            if (_mailFolder?.IsOpen ?? false)
                lock (_mailFolder.SyncRoot)
                    _mailFolder.Close(false, CancellationToken.None);
        }
    }
}
