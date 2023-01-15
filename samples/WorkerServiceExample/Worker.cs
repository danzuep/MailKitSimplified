using MimeKit;
using MailKit;
using MailKit.Search;
using MailKitSimplified.Receiver.Abstractions;
using MailKitSimplified.Receiver.Extensions;
using MailKitSimplified.Receiver.Services;
using MailKitSimplified.Sender.Abstractions;
using System.Diagnostics;

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
        //await GetMessageSummaryRepliesAsync(cancellationToken);
        //await ReceiveAsync(cancellationToken);
        //await QueryAsync(cancellationToken);
        //await MonitorAsync(cancellationToken);
        await NotReentrantAsync(cancellationToken);
    }

    private async Task NotReentrantAsync(CancellationToken cancellationToken = default)
    {
        var sendTask = DelayedSendAsync(5, cancellationToken);

        var newestEmail = await GetNewestMessageSummaryAsync(cancellationToken);
        await _imapReceiver.MonitorFolder
            .SetIgnoreExistingMailOnConnect()
            .OnMessageArrival(OnArrivalAsync)
            .IdleAsync(cancellationToken);

        async Task OnArrivalAsync(IMessageSummary messageSummary)
        {
            try
            {
                if (messageSummary.UniqueId > newestEmail?.UniqueId)
                {
                    var mimeMessage = await messageSummary.GetMimeMessageAsync();
                    //var mimeMessage = await _imapReceiver.ReadMail.GetMimeMessageAsync(messageSummary.UniqueId);
                    _logger.LogInformation($"{_imapReceiver} message #{messageSummary.UniqueId} arrived, {mimeMessage.MessageId}.");
                }
                else
                    _logger.LogInformation($"{_imapReceiver} message #{messageSummary.UniqueId} arrived.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
        }
    }

    public async Task<IMessageSummary?> GetNewestMessageSummaryAsync(CancellationToken cancellationToken = default)
    {
        using var mailFolderClient = _imapReceiver.MailFolderClient;
        var mailFolder = await mailFolderClient.ConnectAsync(false, cancellationToken).ConfigureAwait(false);
        var index = mailFolder.Count - 1;
        var filter = MessageSummaryItems.UniqueId;
        var messageSummaries = await mailFolder.FetchAsync(index, index, filter, cancellationToken).ConfigureAwait(false);
        await mailFolder.CloseAsync(false, CancellationToken.None).ConfigureAwait(false);
        return messageSummaries.FirstOrDefault();
    }

    private async Task GetMessageSummaryRepliesAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var messageSummaries = await _imapReceiver.ReadMail
            .Skip(31).Take(1).Items(MessageSummaryItems.Envelope)
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

    private async Task GetMimeMessageRepliesAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var mimeMessages = await _imapReceiver.ReadMail
            .Take(1).Query(SearchQuery.NotSeen)
            .GetMimeMessagesAsync(cancellationToken);
        stopwatch.Stop();
        _logger.LogInformation($"Received {mimeMessages.Count} email(s) in {stopwatch.Elapsed.TotalSeconds:n1}s.");
        var mimeReply = mimeMessages.Single()
            .GetReplyMessage("Reply here.", addRecipients: false, includeMessageId: true, cancellationToken: cancellationToken)
            .From("from@localhost").To("to@localhost");
        _logger.LogInformation($"Reply: \r\n{mimeReply.HtmlBody}");
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
            .GetMessageSummariesAsync(MailFolderReader.CoreMessageItems, cancellationToken);
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
        _logger.LogInformation($"Email {(isSent ? "sent" : "failed to send")}.");
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