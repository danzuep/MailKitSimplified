using MimeKit;
using MailKit;
using MailKit.Search;
using MailKitSimplified.Receiver.Abstractions;
using MailKitSimplified.Receiver.Extensions;
using MailKitSimplified.Sender.Abstractions;
using System.Diagnostics;
using MailKitSimplified.Receiver.Services;

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
        await GetMessageSummaryRepliesAsync(cancellationToken);
        //await ReceiveAsync(cancellationToken);
        //await QueryAsync(cancellationToken);
        //await MonitorAsync(cancellationToken);
    }

    private async Task GetMessageSummaryRepliesAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var messageSummaries = await _imapReceiver.ReadMail
            .Skip(31).Take(1)
            .Items(MessageSummaryItems.Envelope)
            //.Query(SearchQuery.NotSeen)
            .GetMessageSummariesAsync(cancellationToken);
        stopwatch.Stop();
        _logger.LogInformation($"Received {messageSummaries.Count} email(s) in {stopwatch.Elapsed.TotalSeconds:n1}s: {messageSummaries.Select(m => m.UniqueId).ToEnumeratedString()}.");
        foreach (var messageSummary in messageSummaries)
        {
            if (cancellationToken.IsCancellationRequested)
                break;
            var mimeReply = await messageSummary.GetReplyMessageAsync("Reply here.", addRecipients: false, includeMessageId: true, cancellationToken: cancellationToken);
            mimeReply.From.Add("from@localhost");
            mimeReply.To.Add("to@localhost");
            _logger.LogInformation($"Reply: \r\n{mimeReply.HtmlBody}");
            //await _smtpSender.SendAsync(mimeReply, cancellationToken);
        }
    }

    private async Task ReceiveAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var messageSummaries = await _imapReceiver.ReadMail.Skip(0).Take(250, continuous: true)
            .GetMessageSummariesAsync(MessageSummaryItems.UniqueId, cancellationToken);
        stopwatch.Stop();
        _logger.LogInformation($"Received {messageSummaries.Count} email(s) in {stopwatch.Elapsed.TotalSeconds:n3}s: {messageSummaries.Select(m => m.UniqueId).ToEnumeratedString()}.");
    }

    private async Task QueryAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var messageSummaries = await _imapReceiver.ReadMail.Query(MailFolderReader.QueryMessageId(""))
            .GetMessageSummariesAsync(MessageSummaryItems.Envelope, cancellationToken);
        stopwatch.Stop();
        _logger.LogInformation($"Received {messageSummaries.Count} email(s) in {stopwatch.Elapsed.TotalSeconds:n1}s: {messageSummaries.Select(m => m.UniqueId).ToEnumeratedString()}.");
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

    private async Task MonitorAsync(CancellationToken cancellationToken = default)
    {
        var sendTask = DelayedSendAsync(5, cancellationToken);
        await _imapReceiver.MonitorFolder
            .SetMessageSummaryItems()
            .SetIgnoreExistingMailOnConnect()
            .OnMessageArrival((m) => Console.WriteLine(m.UniqueId))
            .OnMessageDeparture((m) => Console.WriteLine(m.UniqueId))
            .IdleAsync(cancellationToken);
        await sendTask;
    }
}