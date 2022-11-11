using Microsoft.Extensions.Logging;
using MailKit;
using MailKitSimplified.Core.Services;
using MailKitSimplified.Sender.Services;
using MailKitSimplified.Receiver.Services;
using MailKitSimplified.Receiver.Extensions;

using var loggerFactory = LoggerFactory.Create(_ => _.SetMinimumLevel(LogLevel.Trace).AddDebug().AddConsole());
var logger = loggerFactory.CreateLogger<Program>();

using var smtpSender = SmtpSender.Create("localhost", 25);
bool isSent = await smtpSender.WriteEmail
    .Bcc($"{Guid.NewGuid():N}@localhost")
    .TrySendAsync();

var email = BasicEmail.Write.Bcc($"{Guid.NewGuid():N}@localhost").Result;

logger.LogInformation("Email {result}.", isSent ? "sent" : "failed to send");

using var imapReceiver = ImapReceiver.Create("localhost", 143);
var messageSummaries = await imapReceiver.ReadFrom("INBOX")
    .GetMessageSummariesAsync(MessageSummaryItems.UniqueId);

logger.LogDebug("Email(s) received: {ids}", messageSummaries.Select(m => m.UniqueId).ToEnumeratedString());