using MailKit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MailKitSimplified.Sender.Models;
using MailKitSimplified.Sender.Services;
using MailKitSimplified.Receiver.Services;
using MailKitSimplified.Receiver.Extensions;

using var loggerFactory = LoggerFactory.Create(_ => _.SetMinimumLevel(LogLevel.Trace).AddDebug().AddConsole());
var logger = loggerFactory.CreateLogger<Program>();

var senderOptions = Options.Create(new EmailSenderOptions(smtpHost: "localhost", smtpPort: 25));
using var smtpSender = new SmtpSender(senderOptions, loggerFactory.CreateLogger<SmtpSender>());
var writeEmail = smtpSender.WriteEmail;
await writeEmail.To("test@localhost").SendAsync();
bool isSent = await writeEmail
    .From("my.name@example.com")
    .To("YourName@example.com")
    .Subject("Hello World")
    .BodyText("Optional text/plain content.")
    .BodyHtml("Optional text/html content.</br>")
    .TryAttach(@"C:\Temp\EmailClientSmtp.log")
    .TrySendAsync();

logger.LogInformation("Email {result}.", isSent ? "sent" : "failed to send");

using var imapReceiver = ImapReceiver.Create("localhost", 143);
var messageSummaries = await imapReceiver.ReadFrom("INBOX")
    .GetMessageSummariesAsync(MessageSummaryItems.UniqueId);

logger.LogDebug("Email(s) received: {ids}", messageSummaries.Select(m => m.UniqueId).ToEnumeratedString());