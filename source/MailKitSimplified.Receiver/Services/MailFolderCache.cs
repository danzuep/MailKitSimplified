using System;
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
        private static readonly string _inbox = "INBOX";
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
                if (mailFolderFullName.Equals(_inbox, StringComparison.OrdinalIgnoreCase))
                    mailFolder = imapClient.Inbox;
                else
                {
                    int folderCount = await CacheAllMailFoldersAsync(imapReceiver, imapClient).ConfigureAwait(false);
                    if (createIfMissing && !_memoryCache.TryGetValue(key, out mailFolder))
                    {
                        mailFolder = await imapReceiver.MailFolderClient.GetOrCreateFolderAsync(mailFolderFullName, cancellationToken).ConfigureAwait(false);
                        if (mailFolder != null)
                        {
                            var createdKey = GetKey(imapReceiver, mailFolder.FullName);
                            CacheMailFolder(createdKey, mailFolder);
                        }
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

        private async Task<int> CacheAllMailFoldersAsync(IImapReceiver imapReceiver, IImapClient imapClient)
        {
            int folderCount = 0;
            var key = GetKey(imapReceiver, _inbox);
            if (!_memoryCache.TryGetValue(key, out var _))
            {
                CacheMailFolder(key, imapClient.Inbox);
                folderCount++;
            }
            foreach (var mailFolder in await imapClient.GetAllSubfoldersAsync(_cancellationTokenSource.Token).ConfigureAwait(false))
            {
                key = GetKey(imapReceiver, mailFolder.FullName);
                CacheMailFolder(key, mailFolder);
                folderCount++;
            }
            return folderCount;
        }

        public async Task<UniqueId?> MoveToAsync(IImapReceiver imapReceiver, IMessageSummary messageSummary, string destinationFolderFullName, CancellationToken cancellationToken = default)
        {
            if (imapReceiver == null || messageSummary == null || !messageSummary.UniqueId.IsValid)
                return null;
            var source = await GetMailFolderAsync(imapReceiver, messageSummary.Folder.FullName, createIfMissing: false, cancellationToken).ConfigureAwait(false);
            var destination = await GetMailFolderAsync(imapReceiver, destinationFolderFullName, createIfMissing: true, cancellationToken).ConfigureAwait(false);

            bool peekSourceFolder = !source.IsOpen;
            if (!source.IsOpen || source.Access != FolderAccess.ReadWrite)
                await source.OpenAsync(FolderAccess.ReadWrite, cancellationToken).ConfigureAwait(false);
            var resultUid = await source.MoveToAsync(messageSummary.UniqueId, destination, cancellationToken).ConfigureAwait(false);
            if (peekSourceFolder && source.IsOpen)
                await source.CloseAsync(expunge: false, cancellationToken).ConfigureAwait(false);

            return resultUid;
        }

        // Let the Garbage Collector dispose of the injected MemoryCache.
    }
}