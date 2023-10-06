using EmailWpfApp.State.Accounts;
using MailKitSimplified.Receiver.Abstractions;
using MailKitSimplified.Receiver.Services;
using MailKitSimplified.Sender.Abstractions;
using MailKitSimplified.Sender.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EmailWpfApp.State.Authenticators
{
    public class Authenticator : AccountStore, IAuthenticator
    {
        private CancellationTokenSource _cts = new();
        private ISmtpSender? _smtpSender;
        private IImapReceiver? _imapReceiver;
        private readonly ILogger<Authenticator> _logger;

        public Authenticator(ILogger<Authenticator> logger) : base()
        {
            _logger = logger;
        }

        public bool IsLoggedIn => _smtpSender != null && _imapReceiver != null;

        // use with SmtpSender.Create()
        public async Task Login(ISmtpSender? smtpSender = null)
        {
            smtpSender ??= SmtpSender.Create(CurrentSmtpAccount);
            _logger.LogDebug($"Connecting to SMTP {_smtpSender}.");
            try
            {
                _smtpSender = smtpSender;
                await smtpSender.ConnectSmtpClientAsync(_cts.Token);
                _logger.LogDebug($"Connected to SMTP {_smtpSender}.");
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogTrace(ex, "SMTP login cancelled.");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, ex.Message);
                System.Diagnostics.Debugger.Break();
            }
        }

        // use with ImapReceiver.Create()
        public async Task Login(IImapReceiver? imapReceiver = null)
        {
            imapReceiver ??= ImapReceiver.Create(CurrentImapAccount);
            if (imapReceiver != null)
            {
                _logger.LogDebug($"Connecting to IMAP {_imapReceiver}.");
                try
                {
                    _imapReceiver = imapReceiver;
                    await imapReceiver.ConnectAuthenticatedImapClientAsync(_cts.Token);
                    _logger.LogDebug($"Connected to IMAP {_imapReceiver}.");
                }
                catch (OperationCanceledException ex)
                {
                    _logger.LogTrace(ex, "IMAP login cancelled.");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, ex.Message);
                    System.Diagnostics.Debugger.Break();
                }
            }
        }

        public void Logout()
        {
            _smtpSender = null;
            _imapReceiver = null;
        }
    }
}
