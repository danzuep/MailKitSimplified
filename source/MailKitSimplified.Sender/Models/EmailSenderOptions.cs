using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using System;
using System.IO;
using System.IO.Abstractions;
using System.Net;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace MailKitSimplified.Sender.Models
{
    public class EmailSenderOptions
    {
        public const string SectionName = "EmailSender";

        [Required]
        public string SmtpHost { get; set; }
        public ushort SmtpPort { get; set; } = 0;
        public SecureSocketOptions SocketOptions { get; set; } = SecureSocketOptions.Auto;
        public SmtpCapabilities CapabilitiesToRemove { get; set; } = SmtpCapabilities.None;
        public NetworkCredential SmtpCredential { get; set; } = null;
        public string ProtocolLog { get; set; } = null;
        public bool ProtocolLogFileAppend { get; set; } = false;
        public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(1);
        public EmailWriterOptions EmailWriter { get; set; } = new EmailWriterOptions();

        // Constructor required for Configuration mapping.
        public EmailSenderOptions() { }

        public EmailSenderOptions(string smtpHost, NetworkCredential smtpCredential = null, ushort smtpPort = 0, string protocolLog = null, bool protocolLogFileAppend = false)
        {
            if (string.IsNullOrWhiteSpace(smtpHost))
                throw new ArgumentNullException(nameof(smtpHost));
            var hostParts = smtpPort == 0 ? smtpHost.Split(':') : Array.Empty<string>();
            if (hostParts.Length == 2 && ushort.TryParse(hostParts.LastOrDefault(), out smtpPort))
                smtpHost = hostParts.FirstOrDefault();
            if (smtpCredential != null && smtpCredential.UserName == null && smtpCredential.Password == null)
                smtpCredential = null;
            else if (smtpCredential != null && smtpCredential.UserName == null)
                smtpCredential.UserName = string.Empty;

            SmtpHost = smtpHost;
            SmtpPort = smtpPort;
            SmtpCredential = smtpCredential;
            ProtocolLog = protocolLog;
            ProtocolLogFileAppend = protocolLogFileAppend;
        }

        public Task<ISmtpClient> CreateSmtpClientAsync(CancellationToken cancellationToken) =>
            CreateSmtpClientAsync(null, cancellationToken);

        public async Task<ISmtpClient> CreateSmtpClientAsync(IProtocolLogger protocolLogger = null, CancellationToken cancellationToken = default)
        {
            var smtpClient = protocolLogger != null ? new SmtpClient(protocolLogger) : new SmtpClient();
            smtpClient.Timeout = (int)Timeout.TotalMilliseconds;
            await smtpClient.ConnectAsync(SmtpHost, SmtpPort, SecureSocketOptions.Auto, cancellationToken).ConfigureAwait(false);
            await smtpClient.AuthenticateAsync(SmtpCredential, cancellationToken).ConfigureAwait(false);
            return smtpClient;
        }

        public IProtocolLogger CreateProtocolLogger(IFileSystem fileSystem = null)
        {
            IProtocolLogger protocolLogger = null;
            if (ProtocolLog?.Equals("console", StringComparison.OrdinalIgnoreCase) ?? false)
            {
                protocolLogger = new ProtocolLogger(Console.OpenStandardError());
            }
            else if (!string.IsNullOrWhiteSpace(ProtocolLog))
            {
                bool isMockFileSystem = fileSystem != null &&
                    fileSystem.GetType().Name == "MockFileSystem";
                if (fileSystem == null)
                    fileSystem = new FileSystem();
                var directoryName = fileSystem.Path.GetDirectoryName(ProtocolLog);
                if (!string.IsNullOrWhiteSpace(directoryName))
                    fileSystem.Directory.CreateDirectory(directoryName);
                if (isMockFileSystem)
                    protocolLogger = new ProtocolLogger(Stream.Null);
                else
                    protocolLogger = new ProtocolLogger(ProtocolLog, ProtocolLogFileAppend);
            }
            return protocolLogger;
        }

        public EmailSenderOptions Copy() => MemberwiseClone() as EmailSenderOptions;

        public override string ToString() => SmtpHost;
    }
}
