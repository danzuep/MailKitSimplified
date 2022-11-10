using Microsoft.Extensions.Logging;
using MailKit;
using MailKitSimplified.Core.Services;
using MailKitSimplified.Sender.Services;
using MailKitSimplified.Receiver.Services;
using MailKitSimplified.Receiver.Extensions;

using var loggerFactory = LoggerFactory.Create(_ => _.SetMinimumLevel(LogLevel.Trace).AddDebug().AddConsole());
var logger = loggerFactory.CreateLogger<Program>();

using var smtpSender = MimeMessageSender.Create("localhost", 25);
bool isSent = await Email.Create(smtpSender)
    .Bcc($"{Guid.NewGuid():N}@localhost")
    .TrySendAsync();

logger.LogInformation("Email {result}.", isSent ? "sent" : "failed to send");

using var imapReceiver = ImapClientService.Create("localhost", null, 143);
var messages = await MailFolderReader.Create("INBOX", imapReceiver)
    .GetMessageSummariesAsync(MessageSummaryItems.UniqueId);

logger.LogDebug($"Email received: {messages.Select(m => m.UniqueId).ToEnumeratedString()}");