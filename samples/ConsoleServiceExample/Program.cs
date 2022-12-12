using MailKit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;
using MailKitSimplified.Sender.Models;
using MailKitSimplified.Sender.Services;
using MailKitSimplified.Receiver.Models;
using MailKitSimplified.Receiver.Services;
using MailKitSimplified.Receiver.Extensions;

using var loggerFactory = LoggerFactory.Create(_ => _.SetMinimumLevel(LogLevel.Trace).AddDebug().AddConsole()); //.AddSimpleConsole(o => { o.IncludeScopes = true; o.TimestampFormat = "HH:mm:ss.f "; })
var logger = loggerFactory.CreateLogger<Program>();

var configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();

var emailSenderOptions = configuration.GetRequiredSection(EmailSenderOptions.SectionName).Get<EmailSenderOptions>();
using var smtpSender = SmtpSender.Create(emailSenderOptions, loggerFactory.CreateLogger<SmtpSender>()); //SmtpSender.Create("localhost");
var writtenEmail = new EmailWriter(smtpSender, loggerFactory.CreateLogger<EmailWriter>())
    .From("my.name@example.com")
    .To("YourName@example.com")
    .Subject("Hello World")
    .BodyText("Optional text/plain content.")
    .BodyHtml("Optional text/html content.<br/>")
    .Attach("appsettings.json")
    .TryAttach(@"Logs\ImapClient.txt");
bool isSent = await writtenEmail.Copy().TrySendAsync();
logger.LogInformation("Email 1 {result}.", isSent ? "sent" : "failed to send");

isSent = await writtenEmail.Copy().To("new@io").TrySendAsync();
logger.LogInformation("Email 2 {result}.", isSent ? "sent" : "failed to send");

var emailReceiverOptions = Options.Create(configuration.GetRequiredSection(EmailReceiverOptions.SectionName).Get<EmailReceiverOptions>()!);
//var imapLogger = new MailKitProtocolLogger(loggerFactory.CreateLogger<MailKitProtocolLogger>());
using var imapReceiver = new ImapReceiver(emailReceiverOptions, loggerFactory.CreateLogger<ImapReceiver>()); //ImapReceiver.Create("localhost");

var mailFolderReader = imapReceiver.ReadFrom("INBOX").Skip(5).Take(2);
var messageSummaries = await mailFolderReader.GetMessageSummariesAsync(MessageSummaryItems.UniqueId);
//logger.LogDebug("Email(s) received: {fields}", messageSummaries.FirstOrDefault()?.Fields);
logger.LogDebug("Email(s) received: {ids}.", messageSummaries.Select(m => m.UniqueId).ToEnumeratedString());

var mimeMessages = await imapReceiver.ReadMail.Skip(0).Take(1).GetMimeMessagesAsync();
logger.LogDebug("Email(s) received: {ids}.", mimeMessages.Select(m => m.MessageId).ToEnumeratedString());

var imapIdleClient = imapReceiver.MonitorFolder
    .SetMessageSummaryItems().SetIgnoreExistingMailOnConnect()
    .OnMessageArrival((m) => OnArrivalAsync(m))
    .IdleAsync();

Task OnArrivalAsync(IMessageSummary m)
{
    Console.WriteLine(m.UniqueId);
    return Task.CompletedTask;
}
