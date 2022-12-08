using MailKit;
using MailKit.Search;
using MailKitSimplified.Receiver;
using MailKitSimplified.Receiver.Abstractions;
using MailKitSimplified.Receiver.Extensions;
using MailKitSimplified.Sender.Abstractions;

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

    protected override async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var uids = await _imapReceiver.MailFolderClient
            .SearchAsync(SearchQuery.All, cancellationToken);
        var mimeMessage = await _imapReceiver.ReadMail
            .GetMimeMessageAsync(uids.FirstOrDefault());
        var replyBody = mimeMessage.QuoteForReply("Reply here.");
        _logger.LogInformation(replyBody);

        await ReceiveAsync(cancellationToken);

        var sendTask = DelayedSendAsync(5, cancellationToken);
        await _imapReceiver.MonitorFolder
            .OnMessageArrival((m) => Console.WriteLine(m.UniqueId))
            .OnMessageDeparture((m) => Console.WriteLine(m.UniqueId))
            .IdleAsync(cancellationToken);
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
        var messageSummaries = await _imapReceiver.ReadMail
            .Skip(0).Take(250, continuous: true)
            .GetMessageSummariesAsync(MessageSummaryItems.UniqueId, cancellationToken);
        _logger.LogInformation($"Received {messageSummaries.Count} email(s): {messageSummaries.Select(m => m.UniqueId)}.");
    }
}