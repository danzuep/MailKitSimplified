using MailKit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MailKitSimplified.Sender.Models;
using MailKitSimplified.Sender.Services;
using MailKitSimplified.Receiver.Services;
using MailKitSimplified.Receiver.Extensions;
using MailKitSimplified.Receiver.Models;

using var loggerFactory = LoggerFactory.Create(_ => _.SetMinimumLevel(LogLevel.Trace).AddDebug().AddConsole()); //.AddSimpleConsole(o => { o.IncludeScopes = true; o.TimestampFormat = "HH:mm:ss.f "; })
var logger = loggerFactory.CreateLogger<Program>();



var senderOptions = Options.Create(new EmailSenderOptions(smtpHost: "localhost", smtpPort: 25));
using var smtpSender = new SmtpSender(senderOptions, loggerFactory.CreateLogger<SmtpSender>());
bool isSent = await smtpSender.WriteEmail
    .From("my.name@example.com")
    .To("YourName@example.com")
    .Subject("Hello World")
    .BodyText("Optional text/plain content.")
    .BodyHtml("Optional text/html content.<br/>")
    //.TryAttach(@"C:\Temp\EmailImapClientLog.txt")
    .TrySendAsync();
logger.LogInformation("Email {result}.", isSent ? "sent" : "failed to send");



var receiverOptions = Options.Create(new EmailReceiverOptions(imapHost: "localhost", imapPort: 143, protocolLog: @"C:/Temp/Email logs/ImapClient.txt", mailFolderName: "INBOX"));
var imapReceiver = new ImapReceiver(receiverOptions, loggerFactory.CreateLogger<ImapReceiver>(), new MailKitProtocolLogger(loggerFactory.CreateLogger<MailKitProtocolLogger>()));

var mailFolderReader = imapReceiver.ReadFrom("INBOX").Skip(5).Take(2);
var messageSummaries = await mailFolderReader.GetMessageSummariesAsync(MessageSummaryItems.UniqueId);
//logger.LogDebug("Email(s) received: {fields}", messageSummaries.FirstOrDefault()?.Fields);
logger.LogDebug("Email(s) received: {ids}", messageSummaries.Select(m => m.UniqueId).ToEnumeratedString());

var mimeMessages = await imapReceiver.ReadMail.Skip(5).Take(2).GetMimeMessagesAsync();
logger.LogDebug("Email(s) received: {ids}", mimeMessages.Select(m => m.MessageId).ToEnumeratedString());


