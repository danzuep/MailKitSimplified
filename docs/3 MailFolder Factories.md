## ImapReceiverFactory

The ImapReceiverFactory can be used to more easily monitor multiple mailboxes. Follow the [dependency injection setup instructions](Dependency-Injection#ImapReceiverFactory), then you can use the fully configured IImapReceiverFactory. For example:

```csharp
public class EmailService
{
    private readonly ILogger<EmailService> _logger;
    private readonly IImapReceiverFactory _factory;

    public EmailService(IImapReceiverFactory factory, ILogger<EmailService> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    public async Task LogAllMessageSummariesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var receiver in _factory.GetAllImapReceivers())
        {
            cancellationToken.ThrowIfCancellationRequested();
            var messageSummaries = await receiver.ReadMail.GetMessageSummariesAsync(cancellationToken);
            foreach (var messageSummary in messageSummaries)
                _logger.LogInformation($"{receiver} message #{messageSummary.UniqueId}");
        }
    }
}
```

## MailFolderMonitorFactory

The MailFolderMonitorFactory can be used to monitor multiple mailboxes asynchronously in parallel. Follow the [dependency injection setup instructions](Dependency-Injection#MailFolderMonitorFactory), then you can use the fully configured IMailFolderMonitorFactory. For example:

```csharp
public class EmailService
{
    private readonly ILogger<EmailService> _logger;
    private readonly IMailFolderMonitorFactory _factory;

    public EmailService(IMailFolderMonitorFactory factory, ILogger<EmailService> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    public async Task LogAllMessageSummariesAsync(CancellationToken cancellationToken = default)
    {
        void LogUniqueIdArrived(IMessageSummary messageSummary) =>
            _logger.LogInformation($"Message #{messageSummary.UniqueId} arrived.");
        await _factory.MonitorAllMailboxesAsync(LogUniqueIdArrived, cancellationToken);
    }
}
```
