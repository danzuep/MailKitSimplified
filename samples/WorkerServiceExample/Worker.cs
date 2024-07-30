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
    private const string _processed = "Processed";
    private readonly IImapReceiver _imapReceiver;
    private readonly IServiceScope _serviceScope;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<Worker> _logger;

    public Worker(IServiceScopeFactory serviceScopeFactory, ILoggerFactory loggerFactory)
    {
        _serviceScope = serviceScopeFactory.CreateScope();
        _imapReceiver = _serviceScope.ServiceProvider.GetRequiredService<IImapReceiver>();
        _logger = loggerFactory.CreateLogger<Worker>();
        _loggerFactory = loggerFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        using var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        //await ReceiveAsync(cancellationToken);
        //await TemplateSendAsync(1, cancellationToken);
        //await SendAttachmentAsync(500);
        //await ReceiveAsync(cancellationToken);
        //await QueryAsync(cancellationToken);
        //await DeleteSeenAsync(cancellationTokenSource);
        //await NotReentrantAsync(cancellationToken);
        //await DownloadAllAttachmentsAsync(cancellationToken);
        //await ReceiveMimeMessagesContinuouslyAsync(10, cancellationToken);
        //await ImapReceiverFactoryAsync(cancellationToken);
        //await MailFolderMonitorFactoryAsync(cancellationToken);
        //await GetMessageSummaryRepliesAsync(cancellationToken);
        //await GetMimeMessageRepliesAsync(cancellationToken);
        //await AddFlagsToNewestMessageSummaryAsync(cancellationToken);
        //await GetMailFolderCacheAsync();
        //await CreateFolderAndMoveTopOneAsync();
        //await MonitorAsync(cancellationToken);
        await MonitorMoveAsync(cancellationToken);
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
        var imapReceiverFactory = _serviceScope.ServiceProvider.GetRequiredService<IImapReceiverFactory>();
        var receivers = imapReceiverFactory.GetAllImapReceivers();
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
        var mailFolderMonitorFactory = _serviceScope.ServiceProvider.GetRequiredService<IMailFolderMonitorFactory>();
        void LogUniqueIdArrived(IMessageSummary messageSummary) =>
            _logger.LogInformation($"Message #{messageSummary.UniqueId} arrived.");
        await mailFolderMonitorFactory.MonitorAllMailboxesAsync(LogUniqueIdArrived, cancellationToken);
    }

    private async Task MailFolderMonitorMoveFolderAsync(string destinationFolderFullName, CancellationToken cancellationToken = default)
    {
        using var mailFolderClient = _serviceScope.ServiceProvider.GetRequiredService<IMailFolderClient>();
        var mailFolderMonitorFactory = _serviceScope.ServiceProvider.GetRequiredService<IMailFolderMonitorFactory>();
        async Task UniqueIdArrivedAsync(IMessageSummary messageSummary) =>
            await mailFolderClient.MoveToAsync(messageSummary, destinationFolderFullName, cancellationToken);
        await mailFolderMonitorFactory.MonitorAllMailboxesAsync(UniqueIdArrivedAsync, cancellationToken);
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
        var ct = cancellationTokenSource.Token;
        var filteredMessages = await _imapReceiver.ReadMail.Query(SearchQuery.Seen).GetMessageSummariesAsync(ct);
        _logger.LogInformation($"{_imapReceiver} folder query returned {filteredMessages.Count} messages.");
        //var sentFolder = _imapReceiver.MailFolderClient.SentFolder;
        //var messagesDeleted = await _imapReceiver.MailFolderClient.MoveToAsync(
        //    filteredMessages.Select(m => m.UniqueId), sentFolder, ct);
        filteredMessages.ActionEach(async (m) => await _imapReceiver.MailFolderClient.MoveToAsync(m, SpecialFolder.Sent, ct));
        _logger.LogInformation($"Deleted messages from {_imapReceiver} {filteredMessages.Count} Seen messages.");
    }

    private async Task AddToDraftFolderAsync()
    {
        using var smtpSender = _serviceScope.ServiceProvider.GetRequiredService<ISmtpSender>();
        var mimeMessage = CreateTemplate(smtpSender).MimeMessage;
        var draftsFolder = _imapReceiver.MailFolderClient.DraftsFolder;
        var uniqueId = await draftsFolder.AppendAsync(mimeMessage);
        _logger.LogInformation($"Added mime message to {_imapReceiver} {draftsFolder.FullName} folder as #{uniqueId}.");
    }

    private async Task MoveTopOneToFolderAsync(IMailFolderClient mailFolderClient, string destinationFolderFullName, CancellationToken cancellationToken = default)
    {
        var messageSummary = await GetTopMessageSummaryAsync(cancellationToken);
        var uniqueId = await mailFolderClient.MoveToAsync(messageSummary, destinationFolderFullName, cancellationToken);
        //var destinationFolder = await mailFolderClient.GetFolderAsync([destinationFolderFullName], cancellationToken);
        //var uniqueId = await messageSummary.MoveToAsync(destinationFolder, cancellationToken);
        _logger.LogInformation($"Moved {_imapReceiver} mime message {messageSummary.UniqueId} to {destinationFolderFullName} folder as #{uniqueId}.");
    }

    private async Task CreateFolderAndMoveTopOneAsync(string mailFolderFullName = _processed, CancellationToken cancellationToken = default)
    {
        //var mailFolderNames = await _imapReceiver.GetMailFolderNamesAsync(cancellationToken);
        using var mailFolderClient = _serviceScope.ServiceProvider.GetRequiredService<IMailFolderClient>();
        var baseFolder = await mailFolderClient.GetFolderAsync();
        var mailFolder = await baseFolder.GetOrCreateSubfolderAsync(mailFolderFullName, cancellationToken);
        //var mailFolder = await mailFolderClient.GetOrCreateFolderAsync(mailFolderFullName, cancellationToken);
        await MoveTopOneToFolderAsync(mailFolderClient, mailFolderFullName, cancellationToken);
    }

    private async Task GetMailFolderCacheAsync(string mailFolderFullName = _processed, CancellationToken cancellationToken = default)
    {
        using var mailFolderClient = _serviceScope.ServiceProvider.GetRequiredService<IMailFolderClient>();
        var mailFolderCache = _serviceScope.ServiceProvider.GetRequiredService<IMailFolderCache>();
        var folder = await mailFolderCache.GetMailFolderAsync(_imapReceiver, mailFolderFullName, createIfMissing: true, cancellationToken);
        _logger.LogInformation(folder.FullName);
        var messageSummary = await GetTopMessageSummaryAsync(cancellationToken);
        var uniqueId = await mailFolderCache.MoveToAsync(_imapReceiver, messageSummary, mailFolderFullName, cancellationToken);
        //var destinationFolder = await mailFolderClient.GetFolderAsync([destinationFolderFullName], cancellationToken);
        //var uniqueId = await messageSummary.MoveToAsync(destinationFolder, cancellationToken);
        _logger.LogInformation($"Moved {_imapReceiver} mime message #{messageSummary.UniqueId} to {mailFolderFullName} folder as #{uniqueId}.");
    }

    public async Task GetMailFolderAsync(string mailFolderName, CancellationToken cancellationToken = default)
    {
        var messageSummary = await GetTopMessageSummaryAsync(cancellationToken);
        var mailFolder1 = await _imapReceiver.MailFolderClient.GetOrCreateFolderAsync(mailFolderName, cancellationToken);
        var mailFolder2 = await _imapReceiver.MailFolderClient.GetFolderAsync([mailFolderName], cancellationToken);
        var mailFolder3 = await _imapReceiver.ImapClient.Inbox.GetOrCreateSubfolderAsync(mailFolderName, cancellationToken);
        var mailFolder4 = await messageSummary.Folder.GetOrCreateSubfolderAsync(mailFolderName, cancellationToken);
        //var mimeMessage = await messageSummary.GetMimeMessageAsync(cancellationToken);
        _logger.LogInformation($"Mail folder: {mailFolderName}");
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

    private async Task ForwardFirstEmailAsync(CancellationToken cancellationToken = default)
    {
        var extras = MessageSummaryItems.Headers | MessageSummaryItems.Flags | MessageSummaryItems.Size;
        var messageSummaries = await _imapReceiver.ReadMail.Top(1)
            .ItemsForMimeMessages(extras)
            .GetMessageSummariesAsync(cancellationToken);
        foreach (var messageSummary in messageSummaries)
        {
            await ForwardMessageSummaryAsync(messageSummary);
        }
    }

    private async Task ForwardMessageSummaryAsync(IMessageSummary messageSummary, CancellationToken cancellationToken = default)
    {
        using var smtpSender = _serviceScope.ServiceProvider.GetRequiredService<ISmtpSender>();
        var mimeForward = await messageSummary.GetForwardMessageAsync(
            "<p>FYI.</p>", includeMessageId: true, cancellationToken);
        mimeForward.From.Add("from@example.com");
        mimeForward.To.Add("to@example.com");
        _logger.LogInformation($"{_imapReceiver} reply: \r\n{mimeForward.HtmlBody}");
        await smtpSender.SendAsync(mimeForward, cancellationToken);
        await _imapReceiver.MailFolderClient.SentFolder.AppendAsync(mimeForward);
        //await _imapReceiver.MailFolderClient.MoveToAsync(messageSummary, SpecialFolder.Sent, cancellationToken);
        //smtpSender.Enqueue(mimeForward);
    }

    public async Task<MimeMessage?> GetNewestMimeMessageAsync(CancellationToken cancellationToken = default)
    {
        using var mailFolderClient = _imapReceiver.MailFolderClient;
        var messageSummary = await GetNewestMessageSummaryAsync(mailFolderClient, cancellationToken);
        var mimeMessage = await messageSummary.GetMimeMessageAsync(cancellationToken);
        return mimeMessage;
    }

    public async Task<int?> AddFlagsToNewestMessageSummaryAsync(CancellationToken cancellationToken = default)
    {
        using var mailFolderClient = _imapReceiver.MailFolderClient;
        var messageSummary = await GetNewestMessageSummaryAsync(mailFolderClient, cancellationToken);
        if (messageSummary == null) return null;
        var uniqueId = await mailFolderClient.AddFlagsAsync([messageSummary.UniqueId], MessageFlags.Seen);
        return uniqueId;
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
        //using var smtpSender = _serviceScope.ServiceProvider.GetRequiredService<ISmtpSender>();
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
            //await smtpSender.SendAsync(mimeReply, cancellationToken);
        }
    }

    private async Task GetMimeMessageRepliesAsync(CancellationToken cancellationToken = default)
    {
        //using var smtpSender = _serviceScope.ServiceProvider.GetRequiredService<ISmtpSender>();
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
            //await smtpSender.SendAsync(mimeReply);
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

    private async Task<IMessageSummary> GetTopMessageSummaryAsync(CancellationToken cancellationToken = default)
    {
        var messageSummaries = await _imapReceiver.ReadMail.Top(1).GetMessageSummariesAsync(cancellationToken);
        return messageSummaries.First();
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

    private IEmailWriter CreateTemplate(ISmtpSender smtpSender, string from = "me@example.com")
    {
        if (!from.IsEmail())
            _logger.LogWarning($"{from} is not a valid email.");
        var id = $"{Guid.NewGuid():N}"[..8];
        var template = smtpSender.WriteEmail
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
        using var smtpSender = _serviceScope.ServiceProvider.GetRequiredService<ISmtpSender>();
        //var template = await GetTemplate().SaveTemplateAsync();
        //var template = await smtpSender.WithTemplateAsync();
        var template = CreateTemplate(smtpSender);
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
        using var smtpSender = _serviceScope.ServiceProvider.GetRequiredService<ISmtpSender>();
        bool isSent = await CreateTemplate(smtpSender)
            .TryAttach(filePath)
            .TrySendAsync(cancellationToken);
        _logger.LogInformation($"Email {(isSent ? "sent" : "failed to send")}.");
        await Task.Delay(millisecondsDelay, cancellationToken);
    }

    private async Task DelayedSendAsync(int millisecondsDelay, ISmtpSender? smtpSender = null, CancellationToken cancellationToken = default)
    {
        var temporarySender = smtpSender == null;
        smtpSender ??= _serviceScope.ServiceProvider.GetRequiredService<ISmtpSender>();
        await Task.Delay(millisecondsDelay, cancellationToken);
        smtpSender.Enqueue(CreateTemplate(smtpSender).MimeMessage);
        //bool isSent = await CreateTemplate(smtpSender).TrySendAsync(cancellationToken);
        //_logger.LogInformation($"Email {(isSent ? "sent" : "failed to send")}.");
        if (temporarySender) smtpSender.Dispose();
    }

    private async Task NotReentrantAsync(CancellationToken cancellationToken = default)
    {
        using var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        using var smtpSender = _serviceScope.ServiceProvider.GetRequiredService<ISmtpSender>();
        var sendTask = DelayedSendAsync(500, smtpSender, cancellationToken);
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
                    await messageSummary.AddFlagsAsync(MessageFlags.Seen, silent: true, cancellationToken);
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

    private async Task MonitorMoveAsync(CancellationToken cancellationToken = default)
    {
        using var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        using var smtpSender = _serviceScope.ServiceProvider.GetRequiredService<ISmtpSender>();
        using var mailFolderClient = _serviceScope.ServiceProvider.GetRequiredService<IMailFolderClient>();
        int delayMs = 1000;
        var sendTasks = new List<Task>();
        for (int waitCount = 1; waitCount <= 2; waitCount++)
        {
            var sendTask = DelayedSendAsync(waitCount * delayMs, smtpSender, cancellationToken);
            sendTasks.Add(sendTask);
        }
        await _imapReceiver.MonitorFolder
            .SetMessageSummaryItems()
            .SetIgnoreExistingMailOnConnect()
            .OnMessageArrival(ProcessMessageAsync)
            .IdleAsync(cancellationToken);
        await Task.WhenAll(sendTasks);
        _logger.LogInformation($"{_imapReceiver} Monitor & Move test complete.");

        async Task ProcessMessageAsync(IMessageSummary messageSummary)
        {
            var mailFolder = await mailFolderClient.GetOrCreateFolderAsync(_processed, cancellationToken);
            var uniqueId = await mailFolderClient.MoveToAsync(messageSummary.UniqueId, mailFolder, cancellationToken);
            if (uniqueId == null)
                _logger.LogInformation($"{_imapReceiver} message #{messageSummary.UniqueId} not moved to [{_processed}], UniqueId is null.");
            else
                _logger.LogDebug($"{_imapReceiver} message #{messageSummary.UniqueId} moved to [{_processed}] {uniqueId}.");
        }
    }

    private async Task MonitorAsync(CancellationToken cancellationToken = default)
    {
        using var smtpSender = _serviceScope.ServiceProvider.GetRequiredService<ISmtpSender>();
        var sendTask = DelayedSendAsync(500, smtpSender, cancellationToken);
        void ProcessMessage(IMessageSummary messageSummary) =>
            _logger.LogInformation($"{_imapReceiver} message #{messageSummary.UniqueId} processed.");
        await _imapReceiver.MonitorFolder
            .SetMessageSummaryItems()
            .SetIgnoreExistingMailOnConnect()
            .OnMessageArrival(ProcessMessage)
            .OnMessageDeparture(ProcessMessage)
            .IdleAsync(cancellationToken);
        await sendTask;
        _logger.LogInformation($"{_imapReceiver} Monitoring complete.");
    }

    public override void Dispose()
    {
        _serviceScope.Dispose();
        base.Dispose();
    }
}