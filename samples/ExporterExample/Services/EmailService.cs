using Microsoft.Extensions.Logging;
using MailKitSimplified.Receiver.Abstractions;
using MailKitSimplified.Sender.Abstractions;

namespace ExampleNamespace;

public class EmailService
{
    private readonly ILogger<EmailService> _logger;
    private readonly ISmtpSender _smtpSender;
    private readonly IImapReceiver _imapReceiver;

    public EmailService(ISmtpSender smtpSender, IImapReceiver imapReceiver, ILogger<EmailService> logger)
    {
        _logger = logger;
        _smtpSender = smtpSender;
        _imapReceiver = imapReceiver;
    }

    public async Task SendTestAsync(CancellationToken cancellationToken = default)
    {
        bool isSent = await _smtpSender.WriteEmail
            .From("me@localhost")
            .To($"{Guid.NewGuid():N}@localhost")
            .TrySendAsync(cancellationToken);
        _logger.LogInformation("Email {result}.", isSent ? "sent" : "failed to send");
    }

    public async Task ReceiveTestAsync(CancellationToken cancellationToken = default)
    {
        var emails = await _imapReceiver.ReadMail
            .Skip(0).Take(2) //.ReadFrom("INBOX")
            .GetMessageSummariesAsync(cancellationToken);
        _logger.LogInformation("Email(s) received: {emails}.", emails.Select(m => m.UniqueId));
    }
}