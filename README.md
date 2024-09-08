# MailKitSimplified [![Code Size](https://img.shields.io/github/languages/code-size/danzuep/MailKitSimplified)](https://github.com/danzuep/MailKitSimplified)

Sending and receiving emails sounds simple, after all, electronic mail existed [decades](https://en.wikipedia.org/wiki/History_of_email) before the [Internet](https://en.wikipedia.org/wiki/History_of_the_Internet). If you're looking for an all-in-one .NET solution for email, you'll quickly discover [MailKit](https://github.com/jstedfast/MailKit) is recommended by even the likes of [Microsoft](https://learn.microsoft.com/en-us/dotnet/api/system.net.mail.smtpclient?view=net-6.0#remarks) due to how it implements the [RFC standard](https://www.rfc-editor.org/rfc/rfc2822). Unfortunately the downside of doing it all is that MailKit can be difficult to [set up](https://github.com/jstedfast/MailKit#using-mailkit) [and use](https://github.com/jstedfast/MimeKit/blob/master/FAQ.md#messages-1), especially the first time you go to try something like [working with attachments](https://github.com/jstedfast/MimeKit/blob/master/FAQ.md#q-how-do-i-tell-if-a-message-has-attachments) or [writing a reply](https://github.com/jstedfast/MimeKit/blob/master/FAQ.md#q-how-do-i-reply-to-a-message). The aim of this package is to make sending and receiving emails as simple as possible!

## SMTP with MailKitSimplified.Sender [![NuGet](https://img.shields.io/nuget/v/MailKitSimplified.Sender.svg)](https://nuget.org/packages/MailKitSimplified.Sender) [![Downloads](https://img.shields.io/nuget/dt/MailKitSimplified.Sender.svg?style=flat-square)](https://www.nuget.org/packages/MailKitSimplified.Sender)

Sending an email with MailKitSimplified.Sender is as easy as:

```csharp
using var smtpSender = SmtpSender.Create("localhost");
await smtpSender.WriteEmail.To("test@localhost").SendAsync();
```

## IMAP with MailKitSimplified.Receiver [![NuGet](https://img.shields.io/nuget/v/MailKitSimplified.Receiver.svg)](https://nuget.org/packages/MailKitSimplified.Receiver) [![Downloads](https://img.shields.io/nuget/dt/MailKitSimplified.Receiver.svg?style=flat-square)](https://www.nuget.org/packages/MailKitSimplified.Receiver)

Receiving emails with MailKitSimplified.Receiver is as easy as:

```csharp
using var imapReceiver = ImapReceiver.Create("localhost");
var mimeMessages = await imapReceiver.ReadMail.Top(1).GetMimeMessagesAsync();
```

You can even monitor an email folder for new messages asynchronously, never before has it been this easy!

```csharp
await imapReceiver.MonitorFolder.OnMessageArrival(m => Console.WriteLine(m.UniqueId)).IdleAsync();
```

Once you've got either a mime message or a message summary, replying is now equally as intuitive.

```csharp
var mimeReply = mimeMessage.GetReplyMessage("<p>Reply here.</p>").From("noreply@example.com");
```

You're welcome. 🥲

## Example Usage [![Development](https://github.com/danzuep/MailKitSimplified/actions/workflows/development.yml/badge.svg)](https://github.com/danzuep/MailKitSimplified/actions/workflows/development.yml) [![Release](https://github.com/danzuep/MailKitSimplified/actions/workflows/release.yml/badge.svg)](https://github.com/danzuep/MailKitSimplified/actions/workflows/release.yml)

The examples above will actually work with no other setup if you use something like [smtp4dev](https://github.com/rnwood/smtp4dev) (e.g. `docker run -d -p 3000:80 -p 25:25 -p 143:143 rnwood/smtp4dev`), but below are some more realistic examples. The cancellation token is recommended but not required.

### Sending Mail

```csharp
using var smtpSender = SmtpSender.Create("smtp.example.com:587")
    .SetCredential("user@example.com", "App1icati0nP455w0rd")
    .SetProtocolLog("Logs/SmtpClient.txt");
await smtpSender.WriteEmail
    .From("my.name@example.com")
    .To("YourName@example.com")
    .Bcc("admin@example.com")
    .Subject("Hello World")
    .BodyHtml("<p>Hi</p>")
    .Attach("appsettings.json")
    .TryAttach(@"Logs\ImapClient.txt")
    .SendAsync(cancellationToken);
```

See the [MailKitSimplified.Sender wiki](https://github.com/danzuep/MailKitSimplified/wiki/Sender) for more information.

### Receiving Mail

```csharp
using var imapReceiver = ImapReceiver.Create("imap.example.com:993")
    .SetCredential("user@example.com", "App1icati0nP455w0rd")
    .SetProtocolLog("Logs/ImapClient.txt")
    .SetFolder("INBOX");
var mimeMessages = await imapReceiver.ReadMail.Top(10)
    .GetMimeMessagesAsync(cancellationToken);
```

To only download the [message parts](http://www.mimekit.net/docs/html/T_MailKit_MessageSummaryItems.htm) you want to use:

```csharp
var reader = imapReceiver.ReadMail
    .Skip(0).Take(250, continuous: true)
    .Items(MessageSummaryItems.Envelope);
var messageSummaries = await reader.GetMessageSummariesAsync(cancellationToken);
```

Note: MailKit returns results in ascending order by default, use messageSummaries.Reverse() to get descending results or imapReceiver.ReadMail.Top(#) to get the newest results.

To [query](http://www.mimekit.net/docs/html/T_MailKit_Search_SearchQuery.htm) unread emails from the IMAP server:

```csharp
var messageSummaries = await imapReceiver.ReadFrom("INBOX/Subfolder")
    .Query(SearchQuery.NotSeen)
    .ItemsForMimeMessages()
    .GetMessageSummariesAsync(cancellationToken);
```

To asynchronously monitor the mail folder for incoming messages:

```csharp
await imapReceiver.MonitorFolder
    .SetMessageSummaryItems()
    .SetIgnoreExistingMailOnConnect()
    .OnMessageArrival(OnArrivalAsync)
    .IdleAsync(cancellationToken);
```

To asynchronously forward or reply to message summaries as they arrive:

```csharp
async Task OnArrivalAsync(IMessageSummary messageSummary)
{
    var mimeForward = await messageSummary.GetForwardMessageAsync(
        "<p>FYI.</p>", includeMessageId: true);
    mimeForward.From.Add("from@example.com");
    mimeForward.To.Add("to@example.com");
    //logger.LogInformation($"Reply: \r\n{mimeForward.HtmlBody}");
    //await smtpSender.SendAsync(mimeForward, cancellationToken);
}
```

See the [MailKitSimplified.Receiver wiki](https://github.com/danzuep/MailKitSimplified/wiki/Receiver) for more information.

## See Also [![License](https://img.shields.io/github/license/danzuep/MailKitSimplified)](https://github.com/danzuep/MailKitSimplified)

Examples of things like dependency injection, a hosted service, or an ASP.NET API can also be found in the [GitHub samples](https://github.com/danzuep/MailKitSimplified/tree/main/samples).
