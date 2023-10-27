using MailKit;
using MailKit.Net.Imap;
using MailKit.Security;
using System;
using System.Net;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MailKitSimplified.Receiver.Models
{
    public class EmailReceiverOptions
    {
        public const string SectionName = "EmailReceiver";
        private static readonly string _inbox = "INBOX";

        public string MailFolderName
        {
            get => MailFolderNames.FirstOrDefault() ?? _inbox;
            set => MailFolderNames = new List<string> { value };
        }

        public IList<string> MailFolderNames { get; set; } = new List<string> { _inbox };

        public FolderAccess MailFolderAccess { get; set; } = FolderAccess.None;

        [Required]
        public string ImapHost { get; set; }
        public ushort ImapPort { get; set; } = 0;
        public SecureSocketOptions SocketOptions { get; set; } = SecureSocketOptions.Auto;
        public ImapCapabilities CapabilitiesToRemove { get; set; } = ImapCapabilities.None;
        public NetworkCredential ImapCredential { get; set; } = new NetworkCredential();
        public SaslMechanism AuthenticationMechanism { get; set; } = null;
        public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(1);

        public ProtocolLoggerOptions ProtocolLogger { get; set; } = new ProtocolLoggerOptions();

        //[Obsolete("Use ProtocolLogger.FileWrite.FileWritePath or ILogger instead.")]
        public string ProtocolLog
        {
            get => ProtocolLogger.FileWriter.FilePath;
            set => ProtocolLogger.FileWriter.FilePath = value;
        }

        //[Obsolete("Use ProtocolLogger.FileWrite.AppendToExisting or ILogger instead.")]
        public bool ProtocolLogFileAppend
        {
            get => ProtocolLogger.FileWriter.AppendToExisting;
            set => ProtocolLogger.FileWriter.AppendToExisting = value;
        }

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
            ProtocolLogger.FileWriter.FilePath = protocolLog;
            ProtocolLogger.FileWriter.AppendToExisting = protocolLogFileAppend;
        }

        public Task<IImapClient> CreateImapClientAsync(CancellationToken cancellationToken) =>
            CreateImapClientAsync(null, cancellationToken);

        public async Task<IImapClient> CreateImapClientAsync(IProtocolLogger protocolLogger = null, CancellationToken cancellationToken = default)
        {
            var imapClient = protocolLogger != null ? new ImapClient(protocolLogger) : new ImapClient();
            imapClient.Timeout = (int)Timeout.TotalMilliseconds;
            await imapClient.ConnectAsync(ImapHost, ImapPort, SocketOptions, cancellationToken).ConfigureAwait(false);
            if (CapabilitiesToRemove != ImapCapabilities.None)
                imapClient.Capabilities &= ~CapabilitiesToRemove;
            if (imapClient.Capabilities.HasFlag(ImapCapabilities.Compress))
                await imapClient.CompressAsync(cancellationToken).ConfigureAwait(false);
            if (AuthenticationMechanism != null)
                await imapClient.AuthenticateAsync(AuthenticationMechanism).ConfigureAwait(false);
            else
            {
                var ntlm = imapClient.AuthenticationMechanisms.Contains("NTLM") ?
                    new SaslMechanismNtlm(ImapCredential) : null;
                if (ntlm?.Workstation != null)
                    await imapClient.AuthenticateAsync(ntlm, cancellationToken).ConfigureAwait(false);
                else
                    await imapClient.AuthenticateAsync(ImapCredential, cancellationToken).ConfigureAwait(false);
            }
            if (MailFolderAccess != FolderAccess.None)
                await imapClient.Inbox.OpenAsync(MailFolderAccess).ConfigureAwait(false);
            return imapClient;
        }

        public EmailReceiverOptions Copy() => MemberwiseClone() as EmailReceiverOptions;

        public override string ToString() => $"{ImapHost} {ImapCredential.UserName} {MailFolderName}";
    }
}
