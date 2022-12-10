using MailKit;
using MailKit.Search;
using MailKitSimplified.Receiver;
using MailKitSimplified.Receiver.Abstractions;
using MailKitSimplified.Receiver.Extensions;
using MailKitSimplified.Sender.Abstractions;
using MimeKit;

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

        //var sendTask = DelayedSendAsync(5, cancellationToken);
        //await _imapReceiver.MonitorFolder
        //    .SetMessageSummaryItems()
        //    .SetIgnoreExistingMailOnConnect()
        //    .OnMessageArrival((m) => Console.WriteLine(m.UniqueId))
        //    .OnMessageDeparture((m) => Console.WriteLine(m.UniqueId))
        //    .IdleAsync(cancellationToken);
        //await sendTask;
    }

    private async Task GetMessageSummaryRepliesAsync(CancellationToken cancellationToken = default)
    {
        var messageSummaries = await _imapReceiver.ReadMail
            .Skip(30).Take(3).GetMessageSummariesAsync();
        foreach (var messageSummary in messageSummaries)
        {
            if (cancellationToken.IsCancellationRequested) break;
            var mimeReply = await messageSummary.GetReplyMessageAsync("Reply here.");
            mimeReply.From.Add(new MailboxAddress("", "from@localhost"));
            mimeReply.To.Add(new MailboxAddress("", "to@localhost"));
            _logger.LogInformation($"Reply: \r\n{mimeReply.HtmlBody}");
            //await _smtpSender.SendAsync(mimeReply, cancellationToken);
        }
    }

    private async Task GetMimeMessageRepliesAsync(CancellationToken cancellationToken = default)
    {
        var uids = await _imapReceiver.MailFolderClient
            .SearchAsync(SearchQuery.NotSeen, cancellationToken);
        //var mimeMessages = await _imapReceiver.ReadMail.GetMimeMessagesAsync(uids);
        foreach (var uid in uids)
        {
            if (cancellationToken.IsCancellationRequested) break;
            var mimeMessage = await _imapReceiver.ReadMail.GetMimeMessageAsync(uid);
            var mimeReply = mimeMessage.GetReplyMessage("Reply here.");
            mimeReply.From.Add(new MailboxAddress("", "from@localhost"));
            mimeReply.To.Add(new MailboxAddress("", "to@localhost"));
            _logger.LogInformation($"Reply Built. To: {mimeReply.To}.");
            //await _smtpSender.SendAsync(mimeReply, cancellationToken);
        }
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