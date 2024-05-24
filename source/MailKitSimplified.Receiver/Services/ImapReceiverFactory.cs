using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MailKitSimplified.Receiver.Abstractions;
using MailKitSimplified.Receiver.Models;

namespace MailKitSimplified.Receiver.Services
{
    public class ImapReceiverFactory : IImapReceiverFactory
    {
        private readonly IOptionsMonitor<MailboxOptions> _mailboxOptions;
        private readonly ILoggerFactory _loggerFactory;

        public ImapReceiverFactory(IOptionsMonitor<MailboxOptions> mailboxOptions, ILoggerFactory loggerFactory = null)
        {
            _mailboxOptions = mailboxOptions ?? throw new ArgumentNullException(nameof(mailboxOptions));
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
                    if (emailReceiver.MailFolderNames?.Count > 0)
                    {
                        foreach (var mailFolderName in emailReceiver.MailFolderNames)
                        {
                            var options = emailReceiver.Copy();
                            options.MailFolderName = mailFolderName;
                            var imapReceiver = GetImapReceiver(options);
                            imapReceivers.Add(imapReceiver);
                        }
                    }
                    else
                    {
                        var imapReceiver = GetImapReceiver(emailReceiver);
                        imapReceivers.Add(imapReceiver);
                    }
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
            return ImapReceiver.Create(emailReceiverOptions, _loggerFactory.CreateLogger<ImapReceiver>());
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
