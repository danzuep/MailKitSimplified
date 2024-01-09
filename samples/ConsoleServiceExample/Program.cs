using MailKit;
using MailKit.Search;
using Microsoft.Extensions.Options;
using MailKitSimplified.Sender.Models;
using MailKitSimplified.Sender.Services;
using MailKitSimplified.Receiver.Models;
using MailKitSimplified.Receiver.Services;
using MailKitSimplified.Receiver.Extensions;

using var loggerFactory = LoggerFactory.Create(_ => _.SetMinimumLevel(LogLevel.Trace).AddDebug().AddConsole()); //.AddSimpleConsole(o => { o.IncludeScopes = true; o.TimestampFormat = "HH:mm:ss.f "; })
var logger = loggerFactory.CreateLogger<Program>();

var cts = new CancellationTokenSource();
var cancellationToken = cts.Token;

var configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();

var emailSenderOptions = configuration.GetRequiredSection(EmailSenderOptions.SectionName).Get<EmailSenderOptions>();
await using var smtpSender = SmtpSender.Create(emailSenderOptions, loggerFactory.CreateLogger<SmtpSender>()); //SmtpSender.Create("smtp.example.com:587");
//smtpSender.SetCredential("user@example.com", "App1icati0nP455w0rd").SetProtocolLog("Logs/SmtpClient.txt");
//await smtpSender.WriteEmail.To("test@localhost").SendAsync();
var writtenEmail = new EmailWriter(smtpSender, loggerFactory.CreateLogger<EmailWriter>())
    .From("my.name@example.com")
    .To("YourName@example.com")
    .Subject("Hello World")
    .BodyText("Optional text/plain content.")
    .BodyHtml("Optional text/html content.<br/>")
    .Attach("appsettings.json")
    .TryAttach(@"Logs\ImapClient.txt")
    .SaveTemplate(cancellationToken);
bool isSent = await writtenEmail.Copy().TrySendAsync(cancellationToken);
logger.LogInformation("Email 1 {result}.", isSent ? "sent" : "failed to send");

isSent = await writtenEmail.Copy().To("new@io").TrySendAsync(cancellationToken);
logger.LogInformation("Email 2 {result}.", isSent ? "sent" : "failed to send");

var emailReceiverOptions = Options.Create(configuration.GetRequiredSection(EmailReceiverOptions.SectionName).Get<EmailReceiverOptions>()!);
//var imapLogger = new MailKitProtocolLogger(loggerFactory.CreateLogger<MailKitProtocolLogger>());
await using var imapReceiver = new ImapReceiver(emailReceiverOptions, loggerFactory.CreateLogger<ImapReceiver>()); //ImapReceiver.Create("imap.example.com:993");
//imapReceiver.SetCredential("user@example.com", "App1icati0nP455w0rd").SetProtocolLog("Logs/ImapClient.txt").SetFolder("INBOX");
//var mimeMessages = await imapReceiver.ReadMail.Top(10).GetMimeMessagesAsync(cancellationToken);

var mimeMessages = await imapReceiver.ReadMail.Top(1).GetMimeMessagesAsync(cancellationToken);
logger.LogDebug("Email(s) received: {ids}.", mimeMessages.Select(m => m.MessageId).ToEnumeratedString());

var mailFolderReader = imapReceiver.ReadMail
    .Skip(0).Take(25, continuous: true)
    .Items(MessageSummaryItems.Envelope);
var messageSummaries = await mailFolderReader.GetMessageSummariesAsync(cancellationToken);
//logger.LogDebug("Email(s) received: {fields}", messageSummaries.FirstOrDefault()?.Fields);
logger.LogDebug("Email(s) received: {ids}.", messageSummaries.Select(m => m.UniqueId).ToEnumeratedString());

var querySummaries = await imapReceiver.ReadFrom("INBOX/Subfolder")
    .Query(SearchQuery.NotSeen)
    .ItemsForMimeMessages()
    .GetMessageSummariesAsync(cancellationToken);

await imapReceiver.MonitorFolder
    .SetMessageSummaryItems()
    .SetIgnoreExistingMailOnConnect()
    .OnMessageArrival(OnArrivalAsync)
    .IdleAsync(cancellationToken);

async Task OnArrivalAsync(IMessageSummary messageSummary)
{
    var messageReader = await MimeMessageReader.CreateAsync(messageSummary, cancellationToken);
    logger.LogInformation(messageReader.ToString());
    //var mimeForward = await messageSummary.GetForwardMessageAsync("<p>FYI.</p>", includeMessageId: true);
}

var mimeMessage = mimeMessages.FirstOrDefault();
var mimeReply = mimeMessage.GetReplyMessage("<p>Reply here.</p>").From("noreply@example.com");