using MailKitSimplified.Receiver.Abstractions;
using MailKitSimplified.Sender.Abstractions;

namespace ExampleNamespace;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly ISmtpSender _smtpSender;
    private readonly IImapReceiver _imapReceiver;
    private readonly IMailFolderMonitor _mailFolderMonitor;

    public Worker(ISmtpSender smtpSender, IImapReceiver imapReceiver, IMailFolderMonitor mailFolderMonitor, ILogger<Worker> logger)
    {
        _logger = logger;
        _smtpSender = smtpSender;
        _imapReceiver = imapReceiver;
        _mailFolderMonitor = mailFolderMonitor;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken = default)
    {
        await _mailFolderMonitor.MonitorAsync(stoppingToken);
        //await _imapReceiver.Folder("INBOX").MonitorAsync(stoppingToken);
        //await SendAsync(stoppingToken);
        //await ReceiveAsync(stoppingToken);
    }

    private async Task SendAsync(CancellationToken cancellationToken = default)
    {
        var id = $"{Guid.NewGuid():N}";
        bool isSent = await _smtpSender.WriteEmail
            .From("me@localhost")
            .To($"{id}@localhost")
            .Subject(id)
            .BodyText("text/plain.")
            .TrySendAsync(cancellationToken);
        _logger.LogInformation("Email {result}.", isSent ? "sent" : "failed to send");
    }

    private async Task ReceiveAsync(CancellationToken cancellationToken = default)
    {
        var emails = await _imapReceiver.ReadMail
            .Skip(0).Take(10)
            .GetMessageSummariesAsync(cancellationToken);
        _logger.LogInformation("Email(s) received: {emails}.", emails.Select(m => m.UniqueId));
    }
}