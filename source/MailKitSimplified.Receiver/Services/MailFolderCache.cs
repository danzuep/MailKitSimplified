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
        private CancellationTokenSource _cancellationTokenSource;
        private readonly IMemoryCache _memoryCache;
        private readonly IOptionsMonitor<MailboxOptions> _mailboxOptions;

        public MailFolderCache(IMemoryCache memoryCache, IOptionsMonitor<MailboxOptions> mailboxOptions = null)
        {
            _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
            _mailboxOptions = mailboxOptions;
        }

        public async Task<IMailFolder> GetMailFolderAsync(IImapReceiver imapReceiver, string mailFolderFullName, CancellationToken cancellationToken = default)
        {
            if (imapReceiver?.ImapClient == null)
                throw new ArgumentNullException(nameof(imapReceiver));
            var key = $"{imapReceiver} | {mailFolderFullName}";
            if (!_memoryCache.TryGetValue(key, out IMailFolder mailFolder))
            {
                _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                await CacheAllMailFoldersAsync(imapReceiver.ImapClient).ConfigureAwait(false);
                if (!_memoryCache.TryGetValue(key, out mailFolder))
                {
                    CacheMailFolder(mailFolder);
                }
            }
            return mailFolder;
        }

        private void CacheMailFolder(IMailFolder mailFolder)
        {
            _memoryCache.GetOrCreate(mailFolder.FullName,
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
        private async Task CacheAllMailFoldersAsync(IImapClient imapClient)
        {
            await foreach (var mailFolder in imapClient.GetAllSubfoldersAsync(_cancellationTokenSource.Token).ConfigureAwait(false))
            {
                CacheMailFolder(mailFolder);
            }
        }
#else
        private async Task CacheAllMailFoldersAsync(IImapClient imapClient)
        {
            foreach (var mailFolder in await imapClient.GetAllSubfoldersAsync(_cancellationTokenSource.Token).ConfigureAwait(false))
            {
                CacheMailFolder(mailFolder);
            }
        }
#endif

        // Let the Garbage Collector dispose of the injected MemoryCache.
    }
}