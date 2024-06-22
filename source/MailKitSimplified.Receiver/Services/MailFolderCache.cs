using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MailKit;
using MailKit.Net.Imap;
using MailKitSimplified.Receiver.Abstractions;
using MailKitSimplified.Receiver.Extensions;
using MailKitSimplified.Receiver.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace MailKitSimplified.Receiver.Services
{
    public class MailFolderCache : IMailFolderCache
    {
        private CancellationTokenSource _cancellationTokenSource;
        private readonly IMemoryCache _memoryCache;
        private readonly IOptionsMonitor<MailboxOptions> _mailboxOptions;

        public MailFolderCache(IMemoryCache memoryCache, IOptionsMonitor<MailboxOptions> mailboxOptions = null)
        {
            _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
            _mailboxOptions = mailboxOptions;
        }

        public async Task<IMailFolder> GetMailFolderAsync(IImapReceiver imapReceiver, string mailFolderFullName, bool createIfMissing = false, CancellationToken cancellationToken = default)
        {
            if (imapReceiver == null)
                throw new ArgumentNullException(nameof(imapReceiver));
            if (string.IsNullOrWhiteSpace(mailFolderFullName))
                throw new ArgumentNullException(nameof(mailFolderFullName));
            var key = GetKey(imapReceiver, mailFolderFullName);
            if (!_memoryCache.TryGetValue(key, out IMailFolder mailFolder))
            {
                _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                var imapClient = await imapReceiver.ConnectAuthenticatedImapClientAsync(_cancellationTokenSource.Token);
                int folderCount = await CacheAllMailFoldersAsync(imapReceiver, imapClient).ConfigureAwait(false);
                if (folderCount == 0)
                {
                    var inbox = imapClient.Inbox;
                    var inboxKey = GetKey(imapReceiver, inbox.FullName);
                    CacheMailFolder(inboxKey, inbox);
                }
                if (createIfMissing && !_memoryCache.TryGetValue(key, out mailFolder))
                {
                    var namespaceFolder = imapClient.PersonalNamespaces.FirstOrDefault()
                        ?? imapClient.SharedNamespaces.FirstOrDefault()
                        ?? imapClient.OtherNamespaces.FirstOrDefault();
                    var baseFolder = string.IsNullOrEmpty(namespaceFolder?.Path) ?
                        imapClient.Inbox : await imapClient.GetFolderAsync(namespaceFolder.Path);

                    //if (mailFolderFullName.Length >= baseFolder.FullName.Length &&
                    //    mailFolderFullName.Substring(0, baseFolder.FullName.Length) == baseFolder.FullName)
                    //    mailFolderFullName = mailFolderFullName.Substring(baseFolder.FullName.Length).Trim('/');

                    bool peekFolder = !baseFolder?.IsOpen ?? true;
                    _ = await imapReceiver.MailFolderClient.ConnectAsync(true, cancellationToken).ConfigureAwait(false);
                    mailFolder = await baseFolder.CreateAsync(mailFolderFullName, isMessageFolder: true, cancellationToken);
                    if (peekFolder)
                        await baseFolder.CloseAsync(expunge: false, cancellationToken).ConfigureAwait(false);

                    //mailFolder = await imapReceiver.MailFolderClient.GetOrCreateFolderAsync(mailFolderFullName, cancellationToken).ConfigureAwait(false);
                    if (mailFolder != null)
                    {
                        var createdKey = GetKey(imapReceiver, mailFolder.FullName);
                        CacheMailFolder(createdKey, mailFolder);
                    }
                }
            }
            return mailFolder;
        }

        private string GetKey(IImapReceiver imapReceiver, string mailFolderFullName)
        {
            return $"{imapReceiver} | {mailFolderFullName}";
        }

        private void CacheMailFolder(string key, IMailFolder mailFolder)
        {
            _memoryCache.GetOrCreate(key ?? mailFolder.FullName,
                cacheEntry =>
                {
                    if (_mailboxOptions != null)
                    {
                        cacheEntry.SlidingExpiration = _mailboxOptions.CurrentValue.SlidingCacheTime;
                        cacheEntry.AbsoluteExpirationRelativeToNow = _mailboxOptions.CurrentValue.MaximumCacheTime;
                    }
                    return mailFolder;
                });
        }

#if NET5_0_OR_GREATER
        private async Task<int> AsyncCacheAllMailFoldersAsync(IImapReceiver imapReceiver, IImapClient imapClient)
        {
            int folderCount = 0;
            await foreach (var mailFolder in imapClient.AsyncGetAllSubfolders(_cancellationTokenSource.Token).ConfigureAwait(false))
            {
                var key = GetKey(imapReceiver, mailFolder.FullName);
                CacheMailFolder(key, mailFolder);
                folderCount++;
            }
            return folderCount;
        }
#endif
        private async Task<int> CacheAllMailFoldersAsync(IImapReceiver imapReceiver, IImapClient imapClient)
        {
            int folderCount = 0;
            foreach (var mailFolder in await imapClient.GetAllSubfoldersAsync(_cancellationTokenSource.Token).ConfigureAwait(false))
            {
                var key = GetKey(imapReceiver, mailFolder.FullName);
                CacheMailFolder(key, mailFolder);
                folderCount++;
            }
            return folderCount;
        }

        // Let the Garbage Collector dispose of the injected MemoryCache.
    }
}