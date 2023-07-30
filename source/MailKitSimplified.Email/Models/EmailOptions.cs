using System;
using System.Net;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using MailKit;
using MailKit.Net.Smtp;
using MailKit.Net.Imap;
using MailKitSimplified.Sender.Models;
using MailKitSimplified.Receiver.Models;
using MailKitSimplified.Email.Extensions;

namespace MailKitSimplified.Email.Models
{
    public class EmailOptions
    {
        public const string SectionName = "Email";

        public static readonly string Localhost = "localhost";

        public static EmailOptions Default { get; set; } = new EmailOptions();

        [Required]
        public string Host { get; set; } = string.Empty;

        public ushort Port { get; set; } = 0;

        public string Username { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;

        public TimeSpan? TimeLimit { get; set; } = null;

        public Func<IMailService, Task> AuthenticationMethod { get; set; } = null;

        public static EmailOptions Create(string host)
        {
            var options = new EmailOptions().SetHost(host);
            return options;
        }

        public EmailOptions SetHost(string host)
        {
            if (string.IsNullOrWhiteSpace(host))
                throw new ArgumentNullException(nameof(host));
            var array = host.Split(':');
            if (array.Length > 1 && ushort.TryParse(array.LastOrDefault(), out ushort port))
            {
                Host = array.First();
                Port = port;
            }
            return this;
        }

        public EmailOptions SetPort(ushort port = 0)
        {
            Port = port;
            return this;
        }

        public EmailOptions SetCredential(string username, string password)
        {
            Username = username;
            Password = password;
            return this;
        }

        public EmailOptions SetTimeout(TimeSpan? timeout)
        {
            if (timeout.HasValue)
                TimeLimit = timeout.Value;
            return this;
        }

        public EmailOptions SetCredential(NetworkCredential credential)
        {
            Credential = credential;
            return this;
        }

        private readonly int _2minsMs = 120000; // 2 mins in milliseconds
        public int Timeout => (int)(TimeLimit?.TotalMilliseconds ?? _2minsMs);

        public Lazy<SmtpClient> SmtpClient =>
            new Lazy<SmtpClient>(() => new SmtpClient
            {
                Timeout = Timeout
            });

        public Lazy<ImapClient> ImapClient =>
            new Lazy<ImapClient>(() => new ImapClient
            {
                Timeout = Timeout
            });

        public NetworkCredential Credential
        {
            get => new NetworkCredential(Username ?? string.Empty, Password ?? string.Empty);
            set
            {
                if (value != null)
                {
                    Username = value.UserName ?? string.Empty;
                    Password = value.Password ?? string.Empty;
                }
            }
        }

        public EmailSenderOptions EmailSenderOptions
        {
            get => new EmailSenderOptions(Host, Credential, Port); // { Timeout = Timeout };
            set
            {
                if (value != null)
                {
                    Host = value.SmtpHost;
                    Port = value.SmtpPort;
                    Credential = value.SmtpCredential;
                    //Timeout = value.Timeout;
                }
            }
        }

        public EmailReceiverOptions EmailReceiverOptions
        {
            get => new EmailReceiverOptions(Host, Credential, Port); // { Timeout = Timeout };
            set
            {
                if (value != null)
                {
                    Host = value.ImapHost;
                    Port = value.ImapPort;
                    Credential = value.ImapCredential;
                    //Timeout = value.Timeout;
                }
            }
        }

        public Task<SmtpClient> CreateSmtpClientAsync(CancellationToken cancellationToken = default) =>
            CreateAsync<SmtpClient>(cancellationToken);

        public Task<ImapClient> CreateImapClientAsync(CancellationToken cancellationToken = default) =>
            CreateAsync<ImapClient>(cancellationToken);

        private async Task<T> CreateAsync<T>(CancellationToken cancellationToken = default) where T : IMailService, new()
        {
            T client = new T() { Timeout = Timeout };
            await ConnectAuthenticateAsync(client, cancellationToken).ConfigureAwait(false);
            return client;
        }

        public async Task<IMailService> ConnectAuthenticateAsync(IMailService client, CancellationToken cancellationToken = default)
        {
            await client.ConnectAsync(Host, Port, cancellationToken).ConfigureAwait(false);
            await client.AuthenticateAsync(Credential, AuthenticationMethod, cancellationToken).ConfigureAwait(false);
            return client;
        }
    }
}