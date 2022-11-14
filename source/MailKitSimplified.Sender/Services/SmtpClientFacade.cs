using System;
using System.Text;
using System.IO;
using System.IO.Abstractions;
using System.Net;
using System.Net.Sockets;
using System.Net.Security;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MimeKit;
using MailKit;
using MailKit.Security;
using MailKit.Net.Smtp;
using MailKit.Net.Proxy;

namespace MailKitSimplified.Sender.Services
{
    public class SmtpClientFacade : ISmtpClient
    {
        private readonly ISmtpClient _smtpClient;

        public SmtpClientFacade(IProtocolLogger smtpLogger = null)
        {
            _smtpClient = smtpLogger != null ? new SmtpClient(smtpLogger) : new SmtpClient();
        }

        public SmtpCapabilities Capabilities => _smtpClient.Capabilities;

        public string LocalDomain { get => _smtpClient.LocalDomain; set => _smtpClient.LocalDomain = value; }

        public uint MaxSize => _smtpClient.MaxSize;

        public DeliveryStatusNotificationType DeliveryStatusNotificationType { get => _smtpClient.DeliveryStatusNotificationType; set => _smtpClient.DeliveryStatusNotificationType = value; }

        public object SyncRoot => _smtpClient.SyncRoot;

        public SslProtocols SslProtocols { get => _smtpClient.SslProtocols; set => _smtpClient.SslProtocols = value; }
        public X509CertificateCollection ClientCertificates { get => _smtpClient.ClientCertificates; set => _smtpClient.ClientCertificates = value; }
        public bool CheckCertificateRevocation { get => _smtpClient.CheckCertificateRevocation; set => _smtpClient.CheckCertificateRevocation = value; }
        public RemoteCertificateValidationCallback ServerCertificateValidationCallback { get => _smtpClient.ServerCertificateValidationCallback; set => _smtpClient.ServerCertificateValidationCallback = value; }
        public IPEndPoint LocalEndPoint { get => _smtpClient.LocalEndPoint; set => _smtpClient.LocalEndPoint = value; }
        public IProxyClient ProxyClient { get => _smtpClient.ProxyClient; set => _smtpClient.ProxyClient = value; }

        public HashSet<string> AuthenticationMechanisms => _smtpClient.AuthenticationMechanisms;

        public bool IsAuthenticated => _smtpClient.IsAuthenticated;

        public bool IsConnected => _smtpClient.IsConnected;

        public bool IsSecure => _smtpClient.IsSecure;

        public bool IsEncrypted => _smtpClient.IsEncrypted;

        public bool IsSigned => _smtpClient.IsSigned;

        public SslProtocols SslProtocol => _smtpClient.SslProtocol;

        public CipherAlgorithmType? SslCipherAlgorithm => _smtpClient.SslCipherAlgorithm;

        public int? SslCipherStrength => _smtpClient.SslCipherStrength;

        public HashAlgorithmType? SslHashAlgorithm => _smtpClient.SslHashAlgorithm;

        public int? SslHashStrength => _smtpClient.SslHashStrength;

        public ExchangeAlgorithmType? SslKeyExchangeAlgorithm => _smtpClient.SslKeyExchangeAlgorithm;

        public int? SslKeyExchangeStrength => _smtpClient.SslKeyExchangeStrength;

#if NET5_0_OR_GREATER
        public TlsCipherSuite? SslCipherSuite => _smtpClient.SslCipherSuite;

        public CipherSuitesPolicy SslCipherSuitesPolicy { get => _smtpClient.SslCipherSuitesPolicy; set => _smtpClient.SslCipherSuitesPolicy = value; }
#endif

        public int Timeout { get => _smtpClient.Timeout; set => _smtpClient.Timeout = value; }

        public event EventHandler<MessageSentEventArgs> MessageSent
        {
            add { _smtpClient.MessageSent += value; }
            remove { _smtpClient.MessageSent -= value; }
        }

        public event EventHandler<ConnectedEventArgs> Connected
        {
            add { _smtpClient.Connected += value; }
            remove { _smtpClient.Connected -= value; }
        }

        public event EventHandler<DisconnectedEventArgs> Disconnected
        {
            add { _smtpClient.Disconnected += value; }
            remove { _smtpClient.Disconnected -= value; }
        }

        public event EventHandler<AuthenticatedEventArgs> Authenticated
        {
            add { _smtpClient.Authenticated += value; }
            remove { _smtpClient.Authenticated -= value; }
        }

        public void Authenticate(ICredentials credentials, CancellationToken cancellationToken = default) =>
            _smtpClient.Authenticate(credentials, cancellationToken);

        public void Authenticate(Encoding encoding, ICredentials credentials, CancellationToken cancellationToken = default) =>
            _smtpClient.Authenticate(encoding, credentials, cancellationToken);

        public void Authenticate(Encoding encoding, string userName, string password, CancellationToken cancellationToken = default) =>
            _smtpClient.Authenticate(encoding, userName, password, cancellationToken);

        public void Authenticate(string userName, string password, CancellationToken cancellationToken = default) =>
            _smtpClient.Authenticate(userName, password, cancellationToken);

        public void Authenticate(SaslMechanism mechanism, CancellationToken cancellationToken = default) =>
            _smtpClient.Authenticate(mechanism, cancellationToken);

        public Task AuthenticateAsync(ICredentials credentials, CancellationToken cancellationToken = default) =>
            _smtpClient.AuthenticateAsync(credentials, cancellationToken);

        public Task AuthenticateAsync(Encoding encoding, ICredentials credentials, CancellationToken cancellationToken = default) =>
            _smtpClient.AuthenticateAsync(encoding, credentials, cancellationToken);

        public Task AuthenticateAsync(Encoding encoding, string userName, string password, CancellationToken cancellationToken = default) =>
            _smtpClient.AuthenticateAsync(encoding, userName, password, cancellationToken);

        public Task AuthenticateAsync(string userName, string password, CancellationToken cancellationToken = default) =>
            _smtpClient.AuthenticateAsync(userName, password, cancellationToken);

        public Task AuthenticateAsync(SaslMechanism mechanism, CancellationToken cancellationToken = default) =>
            _smtpClient.AuthenticateAsync(mechanism, cancellationToken);

        public void Connect(string host, int port, bool useSsl, CancellationToken cancellationToken = default) =>
            _smtpClient.Connect(host, port, useSsl, cancellationToken);

        public void Connect(string host, int port = 0, SecureSocketOptions options = SecureSocketOptions.Auto, CancellationToken cancellationToken = default) =>
            _smtpClient.Connect(host, port, options, cancellationToken);

        public void Connect(Socket socket, string host, int port = 0, SecureSocketOptions options = SecureSocketOptions.Auto, CancellationToken cancellationToken = default) =>
            _smtpClient.Connect(socket, host, port, options, cancellationToken);

        public void Connect(Stream stream, string host, int port = 0, SecureSocketOptions options = SecureSocketOptions.Auto, CancellationToken cancellationToken = default) =>
            _smtpClient.Connect(stream, host, port, options, cancellationToken);

        public Task ConnectAsync(string host, int port, bool useSsl, CancellationToken cancellationToken = default) =>
            _smtpClient.ConnectAsync(host, port, useSsl, cancellationToken);

        public Task ConnectAsync(string host, int port = 0, SecureSocketOptions options = SecureSocketOptions.Auto, CancellationToken cancellationToken = default) =>
            _smtpClient.ConnectAsync(host, port, options, cancellationToken);

        public Task ConnectAsync(Socket socket, string host, int port = 0, SecureSocketOptions options = SecureSocketOptions.Auto, CancellationToken cancellationToken = default) =>
            _smtpClient.ConnectAsync(socket, host, port, options, cancellationToken);

        public Task ConnectAsync(Stream stream, string host, int port = 0, SecureSocketOptions options = SecureSocketOptions.Auto, CancellationToken cancellationToken = default) =>
            _smtpClient.ConnectAsync(stream, host, port, options, cancellationToken);

        public void Disconnect(bool quit, CancellationToken cancellationToken = default) =>
            _smtpClient.Disconnect(quit, cancellationToken);

        public Task DisconnectAsync(bool quit, CancellationToken cancellationToken = default) =>
            _smtpClient.DisconnectAsync(quit, cancellationToken);

        public InternetAddressList Expand(string alias, CancellationToken cancellationToken = default) =>
            _smtpClient.Expand(alias, cancellationToken);

        public Task<InternetAddressList> ExpandAsync(string alias, CancellationToken cancellationToken = default) =>
            _smtpClient.ExpandAsync(alias, cancellationToken);

        public void NoOp(CancellationToken cancellationToken = default) =>
            _smtpClient.NoOp(cancellationToken);

        public Task NoOpAsync(CancellationToken cancellationToken = default) =>
            _smtpClient.NoOpAsync(cancellationToken);

        public string Send(MimeMessage message, CancellationToken cancellationToken = default, ITransferProgress progress = null) =>
            _smtpClient.Send(message, cancellationToken, progress);

        public string Send(MimeMessage message, MailboxAddress sender, IEnumerable<MailboxAddress> recipients, CancellationToken cancellationToken = default, ITransferProgress progress = null) =>
            _smtpClient.Send(message, sender, recipients, cancellationToken, progress);

        public string Send(FormatOptions options, MimeMessage message, CancellationToken cancellationToken = default, ITransferProgress progress = null) =>
            _smtpClient.Send(options, message, cancellationToken, progress);

        public string Send(FormatOptions options, MimeMessage message, MailboxAddress sender, IEnumerable<MailboxAddress> recipients, CancellationToken cancellationToken = default, ITransferProgress progress = null) =>
            _smtpClient.Send(options, message, sender, recipients, cancellationToken, progress);

        public Task<string> SendAsync(MimeMessage message, CancellationToken cancellationToken = default, ITransferProgress progress = null) =>
            _smtpClient.SendAsync(message, cancellationToken, progress);

        public Task<string> SendAsync(MimeMessage message, MailboxAddress sender, IEnumerable<MailboxAddress> recipients, CancellationToken cancellationToken = default, ITransferProgress progress = null) =>
            _smtpClient.SendAsync(message, sender, recipients, cancellationToken, progress);

        public Task<string> SendAsync(FormatOptions options, MimeMessage message, CancellationToken cancellationToken = default, ITransferProgress progress = null) =>
            _smtpClient.SendAsync(options, message, cancellationToken, progress);

        public Task<string> SendAsync(FormatOptions options, MimeMessage message, MailboxAddress sender, IEnumerable<MailboxAddress> recipients, CancellationToken cancellationToken = default, ITransferProgress progress = null) =>
            _smtpClient.SendAsync(options, message, sender, recipients, cancellationToken, progress);

        public MailboxAddress Verify(string address, CancellationToken cancellationToken = default) =>
            _smtpClient.Verify(address, cancellationToken);

        public Task<MailboxAddress> VerifyAsync(string address, CancellationToken cancellationToken = default) =>
            _smtpClient.VerifyAsync(address, cancellationToken);

        public void Dispose() => _smtpClient?.Dispose();
    }
}