using MailKit;
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MailKitSimplified.Receiver.Abstractions;

namespace MailKitSimplified.Receiver.Services
{
    public class MailFolderClient : IMailFolderClient
    {
        public string MailFolderName => _mailFolder.FullName;
        public int MailFolderCount => _mailFolder.Count;

        private readonly ILogger _logger;
        private readonly IMailFolder _mailFolder;

        public MailFolderClient(IMailFolder mailFolder, ILogger<MailFolderClient> logger = null)
        {
            _logger = logger ?? NullLogger<MailFolderClient>.Instance;
            _mailFolder = mailFolder ?? throw new ArgumentNullException(nameof(mailFolder));
        }

        public async ValueTask<IMailFolder> ConnectAsync(bool enableWrite = false, CancellationToken cancellationToken = default)
        {
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

        public override string ToString() => $"{MailFolderName} ({MailFolderCount})";

        public virtual async ValueTask DisposeAsync()
        {
            if (_mailFolder.IsOpen)
                await _mailFolder.CloseAsync().ConfigureAwait(false);
        }

        public virtual void Dispose()
        {
            if (_mailFolder.IsOpen)
                lock (_mailFolder.SyncRoot)
                    _mailFolder.Close();
        }
    }
}
