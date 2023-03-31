using System;
using System.Net;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using MailKit.Net.Imap;
using MailKit.Security;

namespace MailKitSimplified.Receiver.Models
{
    public class EmailReceiverOptions
    {
        public const string SectionName = "EmailReceiver";
        private static readonly string _inbox = "INBOX";

        public string MailFolderName { get; set; } = _inbox;
        public IList<string> MailFolderNames { get; set; } = new List<string> { _inbox };

        [Required]
        public string ImapHost { get; set; }
        public ushort ImapPort { get; set; } = 0;
        public NetworkCredential ImapCredential { get; set; } = new NetworkCredential();
        public string ProtocolLog { get; set; } = null;
        public bool ProtocolLogFileAppend { get; set; } = false;
        public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(1);

        // Constructor required for Configuration mapping.
        public EmailReceiverOptions() { }

        public EmailReceiverOptions(string imapHost, NetworkCredential imapCredential = null, ushort imapPort = 0, string mailFolderName = null, string protocolLog = null, bool protocolLogFileAppend = false)
        {
            if (string.IsNullOrWhiteSpace(imapHost))
                throw new ArgumentNullException(nameof(imapHost));
            var hostParts = imapPort == 0 ? imapHost.Split(':') : Array.Empty<string>();
            if (hostParts.Length == 2 && ushort.TryParse(hostParts.LastOrDefault(), out imapPort))
                imapHost = hostParts.FirstOrDefault();
            if (imapCredential != null && imapCredential.UserName == null)
                imapCredential.UserName = string.Empty;

            ImapHost = imapHost;
            ImapPort = imapPort;
            if (imapCredential != null)
                ImapCredential = imapCredential;
            if (!string.IsNullOrWhiteSpace(mailFolderName))
                MailFolderName = mailFolderName;
            ProtocolLog = protocolLog;
            ProtocolLogFileAppend = protocolLogFileAppend;
        }

        public async Task<IImapClient> CreateImapClientAsync(CancellationToken cancellationToken = default)
        {
            var imapClient = new ImapClient
            {
                Timeout = (int)Timeout.TotalMilliseconds
            };
            await imapClient.ConnectAsync(ImapHost, ImapPort, SecureSocketOptions.Auto, cancellationToken).ConfigureAwait(false);
            if (imapClient.Capabilities.HasFlag(ImapCapabilities.Compress))
                await imapClient.CompressAsync(cancellationToken).ConfigureAwait(false);
            await imapClient.AuthenticateAsync(ImapCredential, cancellationToken).ConfigureAwait(false);
            //await imapClient.Inbox.OpenAsync(FolderAccess.ReadOnly).ConfigureAwait(false);
            return imapClient;
        }

        public EmailReceiverOptions Copy() => MemberwiseClone() as EmailReceiverOptions;

        public override string ToString() => $"{ImapHost}:{ImapPort} {ImapCredential.UserName} {MailFolderName}";
    }
}
