using MailKit;
using MailKit.Search;
using MailKitSimplified.Receiver;
using MailKitSimplified.Receiver.Abstractions;
using MailKitSimplified.Receiver.Extensions;
using MailKitSimplified.Receiver.Services;
using MailKitSimplified.Sender.Abstractions;
using MailKitSimplified.Sender.Services;

namespace ExampleNamespace;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly ISmtpSender _smtpSender;
    private readonly IImapReceiver _imapReceiver;

    public Worker(ISmtpSender smtpSender, IImapReceiver imapReceiver, ILogger<Worker> logger)
    {
        _logger = logger;
        _smtpSender = smtpSender;
        _imapReceiver = imapReceiver;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken = default)
    {
        var uids = await _imapReceiver.MailFolderClient.SearchAsync(SearchQuery.SubjectContains("Welcome"), stoppingToken);

        var mimeMessages = await _imapReceiver.ReadMail.Take(1).GetMimeMessagesAsync(stoppingToken);
        var replyMessage = mimeMessages.FirstOrDefault().QuoteForReply("Reply here.");

        await new MailFolderMonitor(_imapReceiver).SetProcessMailOnConnect()
            .OnMessageArrival((m) => Console.WriteLine(m.UniqueId))
            .OnMessageDeparture((m) => Console.WriteLine(m.UniqueId))
            .IdleAsync(stoppingToken);

        await ReceiveAsync(stoppingToken);
        var sendTask = DelayedSendAsync(5, stoppingToken);
        await _imapReceiver.MonitorFolder.IdleAsync(stoppingToken);
        await sendTask;
    }

    private async Task DelayedSendAsync(int secondsDelay, CancellationToken cancellationToken = default)
    {
        await Task.Delay(secondsDelay * 1000, cancellationToken);
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
            .Skip(0).Take(10, continuous: true)
            .GetMessageSummariesAsync(cancellationToken);
        _logger.LogInformation("Email(s) received: {emails}.", emails.Select(m => m.UniqueId));
    }
}