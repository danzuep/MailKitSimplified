using CustomServiceExample.Services;
using MailKitSimplified.Sender.Services;
using Microsoft.Extensions.Logging;

using var loggerFactory = LoggerFactory.Create(_ => _.SetMinimumLevel(LogLevel.Trace).AddDebug().AddConsole());
var logger = loggerFactory.CreateLogger<Program>();

using var smtpSender = MimeMessageSender.Create("smtp.example.com");
bool isSent = await MimeMessageWriter.CreateWith(smtpSender)
    .From("me@example.com")
    .To("you@example.com")
    .TrySendAsync();

logger.LogInformation("Email {result}.", isSent ? "sent" : "failed to send");