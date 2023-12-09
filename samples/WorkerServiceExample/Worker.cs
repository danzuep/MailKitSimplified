using CommunityToolkit.Common;
using System.Diagnostics;
using MimeKit;
using MailKit;
using MailKit.Search;
using MailKit.Security;
using MailKitSimplified.Receiver.Abstractions;
using MailKitSimplified.Receiver.Extensions;
using MailKitSimplified.Receiver.Services;
using MailKitSimplified.Sender.Abstractions;

namespace ExampleNamespace;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly ISmtpSender _smtpSender;
    private readonly IImapReceiver _imapReceiver;
    private readonly IImapReceiverFactory _imapReceiverFactory;
    private readonly IMailFolderMonitorFactory _mailFolderMonitorFactory;
    private readonly ILoggerFactory _loggerFactory;

    public Worker(ISmtpSender smtpSender, IImapReceiver imapReceiver, IImapReceiverFactory imapReceiverFactory, IMailFolderMonitorFactory mailFolderMonitorFactory, ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<Worker>();
        _smtpSender = smtpSender;
        _imapReceiver = imapReceiver;
        _imapReceiverFactory = imapReceiverFactory;
        _mailFolderMonitorFactory = mailFolderMonitorFactory;
        _loggerFactory = loggerFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        using var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        //await TemplateSendAsync(1, cancellationToken);
        //await SendAttachmentAsync(500);
        //await ReceiveAsync(cancellationToken);
        //await QueryAsync(cancellationToken);
        //await MonitorAsync(cancellationToken);
        //await DeleteSeenAsync(cancellationTokenSource);
        //await NotReentrantAsync(cancellationToken);
        //await DownloadAllAttachmentsAsync(cancellationToken);
        //await ReceiveMimeMessagesContinuouslyAsync(10, cancellationToken);
        //await ImapReceiverFactoryAsync(cancellationToken);
        //await MailFolderMonitorFactoryAsync(cancellationToken);
        //await GetMessageSummaryRepliesAsync(cancellationToken);
        await GetMimeMessageRepliesAsync(cancellationToken);
    }

    private static ImapReceiver CreateExchangeOAuth2ImapClientExample(SaslMechanismOAuth2 oauth2)
    {
        var imapReceiver = ImapReceiver.Create("localhost:143").SetLogger()
            //.SetPort(993, SecureSocketOptions.SslOnConnect) //"outlook.office365.com:993"
            .SetCustomAuthentication(async (client) => await client.AuthenticateAsync(oauth2));
        return imapReceiver;
    }

    private async Task ImapReceiverFactoryAsync(CancellationToken cancellationToken = default)
    {
        var receivers = _imapReceiverFactory.GetAllImapReceivers();
        foreach (var receiver in receivers)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var messageSummaries = await receiver.ReadMail.GetMessageSummariesAsync(cancellationToken);
            foreach (var messageSummary in messageSummaries)
                _logger.LogInformation($"{receiver} message #{messageSummary.UniqueId}");
        }
    }

    private async Task MailFolderMonitorFactoryAsync(CancellationToken cancellationToken = default)
    {
        void LogUniqueIdArrived(IMessageSummary messageSummary) =>
            _logger.LogInformation($"Message #{messageSummary.UniqueId} arrived.");
        await _mailFolderMonitorFactory.MonitorAllMailboxesAsync(LogUniqueIdArrived, cancellationToken);
    }

    private async Task DownloadEmailAsync(string filePath = "download.eml", CancellationToken cancellationToken = default)
    {
        var mimeMessage = await GetNewestMimeMessageAsync(cancellationToken);
        string downloadFilePath = Path.GetFullPath(filePath);
        await MimeMessageReader.Create(mimeMessage).SetLogger(_loggerFactory)
            .SaveAsync(downloadFilePath, false, cancellationToken);
    }

    private async Task DownloadAllAttachmentsAsync(CancellationToken cancellationToken = default)
    {
        var mimeMessage = await GetNewestMimeMessageAsync(cancellationToken);
        string downloadFolder = Path.GetFullPath("Downloads");
        _logger.LogInformation($"Downloading attachments from {mimeMessage?.MessageId} into {downloadFolder}.");
        var downloads = await MimeMessageReader.Create(mimeMessage).SetLogger(_loggerFactory)
            .DownloadAllAttachmentsAsync(downloadFolder, createDirectory: true);
        _logger.LogInformation($"Downloads ({downloads.Count}): {downloads.ToEnumeratedString()}.");
    }

    private async Task MoveSeenToSentAsync(CancellationTokenSource cancellationTokenSource)
    {
        var filteredMessages = await _imapReceiver.ReadMail.Query(SearchQuery.Seen)
            .GetMessageSummariesAsync(cancellationTokenSource.Token);
        _logger.LogInformation($"{_imapReceiver} folder query returned {filteredMessages.Count} messages.");
        //var sentFolder = _imapReceiver.MailFolderClient.SentFolder;
        //var messagesDeleted = await _imapReceiver.MailFolderClient.MoveToAsync(
        //    filteredMessages.Select(m => m.UniqueId), sentFolder, cancellationTokenSource.Token);
        filteredMessages.ActionEach(async (m) => await _imapReceiver.MoveToSentAsync(m, cancellationTokenSource.Token));
        _logger.LogInformation($"Deleted messages from {_imapReceiver} {filteredMessages.Count} Seen messages.");
    }

    private async Task MoveTopOneToDraftAsync()
    {
        var mimeMessage = CreateTemplate().MimeMessage;
        var draftsFolder = _imapReceiver.MailFolderClient.DraftsFolder;
        var uniqueId = await draftsFolder.AppendAsync(mimeMessage);
        _logger.LogInformation($"Added mime message to {_imapReceiver} {draftsFolder.FullName} folder as #{uniqueId}.");
    }

    private async Task DeleteSeenAsync(CancellationTokenSource cancellationTokenSource)
    {
        var filteredMessages = await _imapReceiver.ReadMail.Query(SearchQuery.Seen)
            .GetMessageSummariesAsync(cancellationTokenSource.Token);
        _logger.LogInformation($"{_imapReceiver} folder query returned {filteredMessages.Count} messages.");
        var messagesDeleted = await _imapReceiver.MailFolderClient
            .DeleteMessagesAsync(TimeSpan.Zero, SearchQuery.Seen, cancellationTokenSource.Token);
        _logger.LogInformation($"Deleted {messagesDeleted} messages from {_imapReceiver} {filteredMessages.Count} Seen messages.");
    }

    private async Task NotReentrantAsync(CancellationToken cancellationToken = default)
    {
        using var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var sendTask = DelayedSendAsync(500, cancellationToken);
        var newestEmail = await GetNewestMessageSummaryAsync();
        await _imapReceiver.MonitorFolder.SetMessageSummaryItems()
            .SetIgnoreExistingMailOnConnect()
            .OnMessageArrival(OnArrivalAsync)
            .IdleAsync(cancellationTokenSource.Token);
        await sendTask;
        _logger.LogInformation($"{_imapReceiver} NotReentrant test complete.");

        async Task OnArrivalAsync(IMessageSummary messageSummary)
        {
            try
            {
                if (messageSummary.UniqueId.Id > newestEmail?.UniqueId.Id)
                {
                    var mimeMessage = await messageSummary.GetMimeMessageAsync(cancellationToken);
                    //var mimeMessage = await _imapReceiver.ReadMail.GetMimeMessageAsync(messageSummary.UniqueId);
                    await messageSummary.AddFlagsAsync(MessageFlags.Seen);
                    _logger.LogDebug($"{_imapReceiver} message #{messageSummary.UniqueId} message downloaded, Seen flag added.");
                    _logger.LogInformation($"{_imapReceiver} message #{messageSummary.UniqueId} arrival processed, {mimeMessage.MessageId}.");
                    cancellationTokenSource.Cancel();
                }
                else
                    _logger.LogInformation($"{_imapReceiver} message #{messageSummary.UniqueId} arrived.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{_imapReceiver} message #{messageSummary.UniqueId} failed. {ex.Message}");
            }
        }
    }

    private async Task ForwardFirstEmailAsync(CancellationToken cancellationToken = default)
    {
        var extras = MessageSummaryItems.Headers | MessageSummaryItems.Flags | MessageSummaryItems.Size;
        var messageSummaries = await _imapReceiver.ReadMail.Top(1)
            .ItemsForMimeMessages(extras)
            .GetMessageSummariesAsync(cancellationToken);
        foreach (var messageSummary in messageSummaries)
        {
            await ForwardMessageSummaryAsync(messageSummary);
            await _imapReceiver.MoveToSentAsync(messageSummary, cancellationToken);
        }
    }

    private async Task ForwardMessageSummaryAsync(IMessageSummary messageSummary, CancellationToken cancellationToken = default)
    {
        var mimeForward = await messageSummary.GetForwardMessageAsync(
            "<p>FYI.</p>", includeMessageId: true, cancellationToken);
        mimeForward.From.Add("from@example.com");
        mimeForward.To.Add("to@example.com");
        _logger.LogInformation($"{_imapReceiver} reply: \r\n{mimeForward.HtmlBody}");
        await _smtpSender.SendAsync(mimeForward, cancellationToken);
        //_smtpSender.Enqueue(mimeForward);
    }

    public async Task<MimeMessage?> GetNewestMimeMessageAsync(CancellationToken cancellationToken = default)
    {
        using var mailFolderClient = _imapReceiver.MailFolderClient;
        var messageSummary = await GetNewestMessageSummaryAsync(mailFolderClient, cancellationToken);
        var mimeMessage = await messageSummary.GetMimeMessageAsync(cancellationToken);
        return mimeMessage;
    }

    public async Task<IMessageSummary?> GetNewestMessageSummaryAsync(IMailFolderClient? mailFolderClient = null, CancellationToken cancellationToken = default)
    {
        bool dispose = mailFolderClient == null;
        mailFolderClient ??= _imapReceiver.MailFolderClient;
        var mailFolder = await mailFolderClient.ConnectAsync(false, cancellationToken).ConfigureAwait(false);
        var index = mailFolder.Count > 0 ? mailFolder.Count - 1 : mailFolder.Count;
        var filter = MessageSummaryItems.UniqueId;
        var messageSummaries = await mailFolder.FetchAsync(index, index, filter, cancellationToken).ConfigureAwait(false);
        await mailFolder.CloseAsync(false, CancellationToken.None).ConfigureAwait(false);
        var messageSummary = messageSummaries.FirstOrDefault();
        if (dispose)
            await mailFolderClient.DisposeAsync();
        return messageSummary;
    }

    private async Task GetMessageSummaryRepliesAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var messageSummaries = await _imapReceiver.ReadMail
            .Skip(0).Take(1).ItemsForMimeMessages()
            .GetMessageSummariesAsync(cancellationToken);
        stopwatch.Stop();
        _logger.LogDebug($"{_imapReceiver} received {messageSummaries.Count} email(s) in {stopwatch.Elapsed.TotalSeconds:n1}s: {messageSummaries.Select(m => m.UniqueId).ToEnumeratedString()}.");
        foreach (var messageSummary in messageSummaries)
        {
            if (cancellationToken.IsCancellationRequested)
                break;
            var mimeReply = await messageSummary.GetReplyMessageAsync(
                "Reply here.", addRecipients: false, includeMessageId: true, cancellationToken: cancellationToken);
            mimeReply.From.Add("from@localhost");
            mimeReply.To.Add("to@localhost");
            _logger.LogInformation($"{_imapReceiver} reply: \r\n{mimeReply.HtmlBody}");
            //await _smtpSender.SendAsync(mimeReply, cancellationToken);
        }
    }

    private async Task GetMimeMessageRepliesAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var mimeMessages = await _imapReceiver.ReadMail.Top(1)
            .GetMimeMessagesEnvelopeBodyAsync(cancellationToken);
        stopwatch.Stop();
        _logger.LogDebug($"{_imapReceiver} received {mimeMessages.Count} email(s) in {stopwatch.Elapsed.TotalSeconds:n1}s.");
        foreach (var mimeMessage in mimeMessages)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var mimeReply = mimeMessage.GetReplyMessage("Reply here.", addRecipients: false, replyToAll: false, includeMessageId: true, cancellationToken: cancellationToken)
                .From("from@localhost")
                .To("to@localhost");
            _logger.LogInformation($"{_imapReceiver} reply: \r\n{mimeReply.HtmlBody}");
            //await _smtpSender.SendAsync(mimeReply);
        }
    }

    private void ProcessMessages(IList<IMessageSummary> messageSummaries, CancellationToken cancellationToken = default)
    {
        foreach (var messageSummary in messageSummaries)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _logger.LogInformation($"Processed message #{messageSummary.UniqueId}");
        }
    }

    private void ProcessMessages(IList<MimeMessage> mimeMessages, CancellationToken cancellationToken = default)
    {
        foreach (var mimeMessage in mimeMessages)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _logger.LogInformation($"Processed {mimeMessage.MessageId}");
        }
    }

    private async Task ReceiveContinuouslyAsync(CancellationToken cancellationToken = default)
    {
        int count;
        do
        {
            var messageSummaries = await _imapReceiver.ReadMail
                .Take(250, continuous: true)
                .ItemsForMimeMessages()
                .GetMessageSummariesAsync(cancellationToken);
            count = messageSummaries.Count;
            ProcessMessages(messageSummaries, cancellationToken);
        }
        while (count > 0);
    }

    private async Task ReceiveMessageSummariesContinuouslyAsync(ushort batchSize, MessageSummaryItems filter = MessageSummaryItems.UniqueId, CancellationToken cancellationToken = default)
    {
        var reader = _imapReceiver.ReadMail.Range(UniqueId.MinValue, batchSize);
        IList<IMessageSummary> messageSummaries;
        do
        {
            messageSummaries = await reader.ItemsForMimeMessages()
                .GetMessageSummariesAsync(cancellationToken);
            ProcessMessages(messageSummaries, cancellationToken);
        }
        while (messageSummaries.Count > 0);
    }

    private async Task ReceiveMimeMessagesContinuouslyAsync(ushort batchSize, CancellationToken cancellationToken = default)
    {
        var reader = _imapReceiver.ReadMail.Range(UniqueId.MinValue, batchSize);
        IList<MimeMessage> mimeMessages;
        do
        {
            mimeMessages = await reader.GetMimeMessagesAsync(cancellationToken);
            ProcessMessages(mimeMessages, cancellationToken);
        }
        while (mimeMessages.Count > 0);
    }

    private async Task ReceiveMimeMessagesEnvelopeBodyContinuouslyAsync(ushort batchSize, CancellationToken cancellationToken = default)
    {
        var reader = _imapReceiver.ReadMail.Range(UniqueId.MinValue, batchSize);
        IList<MimeMessage> mimeMessages;
        do
        {
            mimeMessages = await reader.GetMimeMessagesEnvelopeBodyAsync(cancellationToken);
            ProcessMessages(mimeMessages, cancellationToken);
        }
        while (mimeMessages.Count > 0);
    }

    private async Task<MimeMessage?> ReceiveMimeMessageAsync(UniqueId uniqueId, CancellationToken cancellationToken = default)
    {
        var mimeMessages = await _imapReceiver.ReadMail.Range(uniqueId, uniqueId)
            .GetMimeMessagesEnvelopeBodyAsync(cancellationToken);
        return mimeMessages.FirstOrDefault();
    }

    private async Task ReceiveAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        //ImapReceiver.Create(new EmailReceiverOptions(), null, null);
        //new ImapReceiver(Options.Create(new EmailReceiverOptions()), logger, new LogFileWriter(), client, loggerFactory);
        var messageSummaries = await _imapReceiver.ReadMail
            .Skip(0).Take(250, continuous: true)
            .GetMessageSummariesAsync(cancellationToken);
        stopwatch.Stop();
        _logger.LogInformation($"{_imapReceiver} received {messageSummaries.Count} email(s) in {stopwatch.Elapsed.TotalSeconds:n3}s: {messageSummaries.Select(m => m.UniqueId).ToEnumeratedString()}.");
    }

    private async Task QueryAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var messageSummaries = await _imapReceiver.ReadMail
            .QueryMessageId("")
            .Items(MessageSummaryItems.Envelope)
            .GetMessageSummariesAsync(cancellationToken);
        stopwatch.Stop();
        _logger.LogInformation($"{_imapReceiver} received {messageSummaries.Count} email(s) in {stopwatch.Elapsed.TotalSeconds:n1}s: {messageSummaries.Select(m => m.UniqueId).ToEnumeratedString()}.");
    }

    private IEmailWriter CreateTemplate(string from = "me@example.com")
    {
        if (!from.IsEmail())
            _logger.LogWarning($"{from} is not a valid email.");
        var id = $"{Guid.NewGuid():N}"[..8];
        var template = _smtpSender.WriteEmail
            .From(from)
            .To($"{id}@localhost")
            .Subject(id)
            .BodyText("text/plain.")
            .BodyHtml("text/html.")
            .SaveTemplate();
        return template;
    }
    
    private async Task TemplateSendAsync(byte numberToSend = 1, CancellationToken cancellationToken = default)
    {
        //var template = await GetTemplate().SaveTemplateAsync();
        //var template = await _smtpSender.WithTemplateAsync();
        var template = CreateTemplate();
        int count = 0;
        do
        {
            bool isSent = await template.TrySendAsync(cancellationToken);
            _logger.LogInformation($"Email {(isSent ? "sent" : "failed to send")}.");
            count++;
        }
        while (count < numberToSend);
    }

    private async Task SendAttachmentAsync(int millisecondsDelay, string filePath = "..\\..\\README.md", CancellationToken cancellationToken = default)
    {
        bool isSent = await CreateTemplate()
            .TryAttach(filePath)
            .TrySendAsync(cancellationToken);
        _logger.LogInformation($"Email {(isSent ? "sent" : "failed to send")}.");
        await Task.Delay(millisecondsDelay, cancellationToken);
    }

    private async Task DelayedSendAsync(int millisecondsDelay, CancellationToken cancellationToken = default)
    {
        await Task.Delay(millisecondsDelay, cancellationToken);
        bool isSent = await CreateTemplate().TrySendAsync(cancellationToken);
        _logger.LogInformation($"Email {(isSent ? "sent" : "failed to send")}.");
    }

    private async Task MonitorAsync(CancellationToken cancellationToken = default)
    {
        var sendTask = DelayedSendAsync(1000, cancellationToken);
        await _imapReceiver.MonitorFolder
            .SetMessageSummaryItems()
            .SetIgnoreExistingMailOnConnect()
            .OnMessageArrival((m) => Console.WriteLine(m.UniqueId))
            .OnMessageDeparture((m) => Console.WriteLine(m.UniqueId))
            .IdleAsync(cancellationToken);
        await sendTask;
    }
}