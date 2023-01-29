using System;
using Microsoft.Extensions.Options;
using MailKitSimplified.Receiver.Abstractions;
using MailKitSimplified.Receiver.Models;
using MailKitSimplified.Receiver.Services;

namespace MailKitSimplified.Receiver.Factories
{
    public class ImapReceiverClientFactory : IImapReceiverClientFactory
    {
        private readonly IOptionsSnapshot<EmailReceiverOptions> _emailReceiverOptionsSnapshot;

        public ImapReceiverClientFactory(IOptionsSnapshot<EmailReceiverOptions> emailReceiverOptionsSnapshot)
        {
            _emailReceiverOptionsSnapshot = emailReceiverOptionsSnapshot;
        }

        public IImapReceiver GetClient(string clientId)
        {
            if (string.IsNullOrWhiteSpace(clientId))
            {
                throw new ArgumentNullException(nameof(clientId));
            }

            //var clientOptions = _emailReceiverOptionsSnapshot?.Value.ImapClients;
            //var client = clientOptions?.FirstOrDefault(c => c.ClientId == clientId);
            var client = _emailReceiverOptionsSnapshot?.Value;
            if (client == null)
            {
                throw new NullReferenceException($"No configuration of type {EmailReceiverOptions.SectionName}:{clientId} was supplied");
            }

            var httpClient = ImapReceiver.Create(client);

            return httpClient;
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}