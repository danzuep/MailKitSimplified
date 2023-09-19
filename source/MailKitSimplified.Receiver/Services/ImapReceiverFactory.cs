using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Caching.Memory;
using MailKitSimplified.Receiver.Abstractions;
using MailKitSimplified.Receiver.Models;

namespace MailKitSimplified.Receiver.Services
{
    public class ImapReceiverFactory : IImapReceiverFactory
    {
        private readonly IOptionsMonitor<MailboxOptions> _mailboxOptions;
        private readonly IMemoryCache _memoryCache;
        private readonly ILoggerFactory _loggerFactory;

        public ImapReceiverFactory(IOptionsMonitor<MailboxOptions> mailboxOptions, IMemoryCache memoryCache, ILoggerFactory loggerFactory = null)
        {
            _mailboxOptions = mailboxOptions ?? throw new ArgumentNullException(nameof(mailboxOptions));
            _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
            _loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
        }

        public IList<IImapReceiver> GetAllImapReceivers()
        {
            var imapReceivers = new List<IImapReceiver>();
            var emailReceiverOptions = _mailboxOptions?.CurrentValue.EmailReceivers;
            if (emailReceiverOptions != null)
            {
                foreach (var emailReceiver in emailReceiverOptions)
                {
                    var imapReceiver = GetImapReceiver(emailReceiver);
                    imapReceivers.Add(imapReceiver);
                }
            }
            return imapReceivers;
        }

        public IImapReceiver GetImapReceiver(string imapHost)
        {
            var emailReceiverOptions = GetEmailReceiverOptions(imapHost);
            var imapReceiver = GetImapReceiver(emailReceiverOptions);
            return imapReceiver;
        }

        internal IImapReceiver GetImapReceiver(EmailReceiverOptions emailReceiverOptions)
        {
            var imapHost = emailReceiverOptions.ImapHost;
            var cachedValue = _memoryCache.GetOrCreate(imapHost,
                cacheEntry =>
                {
                    if (_mailboxOptions != null)
                    {
                        cacheEntry.SlidingExpiration = _mailboxOptions.CurrentValue.SlidingCacheTime;
                        cacheEntry.AbsoluteExpirationRelativeToNow = _mailboxOptions.CurrentValue.MaximumCacheTime;
                    }
                    var imapReceiver = ImapReceiver.Create(emailReceiverOptions, _loggerFactory.CreateLogger<ImapReceiver>());
                    return imapReceiver;
                });
            return cachedValue;
        }

        private EmailReceiverOptions GetEmailReceiverOptions(string imapHost)
        {
            if (string.IsNullOrWhiteSpace(imapHost))
            {
                throw new ArgumentNullException(nameof(imapHost));
            }
            var emailReceivers = _mailboxOptions?.CurrentValue.EmailReceivers;
            var emailReceiverOptions = emailReceivers?.SingleOrDefault(c => c.ImapHost == imapHost);
            if (emailReceiverOptions == null)
            {
                throw new ArgumentException($"No {MailboxOptions.SectionName} configuration was found for {imapHost}.");
            }
            return emailReceiverOptions;
        }

        // Let the Garbage Collector dispose of the injected LoggerFactory and MemoryCache.
    }
}