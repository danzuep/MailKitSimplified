using MailKitSimplified.Receiver.Abstractions;
using MailKitSimplified.Sender.Abstractions;

namespace ExampleNamespace;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly ISmtpSender _smtpSender;
    private readonly IImapReceiver _imapReceiver;

    public Worker(ILogger<Worker> logger, ISmtpSender smtpSender, IImapReceiver imapReceiver)
    {
        _logger = logger;
        _smtpSender = smtpSender;
        _imapReceiver = imapReceiver;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken = default)
    {
        await SendAsync(stoppingToken);
        await ReceiveAsync(stoppingToken);
    }

    private async Task ReceiveAsync(CancellationToken cancellationToken = default)
    {
        var emails = await _imapReceiver.ReadMail
            .Skip(0).Take(10)
            .GetMessageSummariesAsync(cancellationToken);
        _logger.LogInformation("Email(s) received: {emails}.", emails.Select(m => m.UniqueId));
    }

    private async Task SendAsync(CancellationToken cancellationToken = default)
    {
        bool isSent = await _smtpSender.WriteEmail
            .From("me@localhost")
            .To($"{Guid.NewGuid():N}@localhost")
            .TrySendAsync(cancellationToken);
        _logger.LogInformation("Email {result}.", isSent ? "sent" : "failed to send");
    }
}