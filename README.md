# MailKitSimplified [![Development](https://github.com/danzuep/MailKitSimplified/actions/workflows/development.yml/badge.svg)](https://github.com/danzuep/MailKitSimplified/actions/workflows/development.yml) [![Release](https://github.com/danzuep/MailKitSimplified/actions/workflows/release.yml/badge.svg)](https://github.com/danzuep/MailKitSimplified/actions/workflows/release.yml)

Sending and receiving emails sounds simple, after all, electronic mail existed [decades](https://en.wikipedia.org/wiki/History_of_email) before the [Internet](https://en.wikipedia.org/wiki/History_of_the_Internet). If you're looking for an all-in-one .NET solution for email, you'll quickly discover [MailKit](https://github.com/jstedfast/MailKit) is recommended by even the likes of [Microsoft](https://learn.microsoft.com/en-us/dotnet/api/system.net.mail.smtpclient?view=net-6.0#remarks) due to how it implements the [RFC standard](https://www.rfc-editor.org/rfc/rfc2822). Unfortunately the downside of doing it all is that MailKit can be difficult to [set up](https://github.com/jstedfast/MailKit#using-mailkit) [and use](https://github.com/jstedfast/MimeKit/blob/master/FAQ.md#messages-1), especially the first time you go to try something like [working with attachments](https://github.com/jstedfast/MimeKit/blob/master/FAQ.md#q-how-do-i-tell-if-a-message-has-attachments) or [writing a reply](https://github.com/jstedfast/MimeKit/blob/master/FAQ.md#q-how-do-i-reply-to-a-message). The aim of this package is to make sending and receiving emails as simple as possible!

Sending an email is now as easy as:

```csharp
await smtpSender.WriteEmail.To("test@localhost").SendAsync();
```

Receiving emails is now as easy as:

```csharp
var mimeMessages = await imapReceiver.ReadFrom("INBOX").GetMimeMessagesAsync();
```

## MailKitSimplified.Sender Usage

### Setup

If you're not familiar with dependency injection then you can specify the SMTP host address like this:

```csharp
using var smtpSender = SmtpSender.Create("smtp.example.com");
```

An email sender must have a SMTP host address, and sometimes a port number, but leaving the port as the default value of 0 will normally choose the right port automatically (e.g. 25). Most companies use LDAP or something similar for behind-the-scenes authentication, but if not you can specify a network credential too.

### Sending Mail

```csharp
await smtpSender.WriteEmail
    .From("my.name@example.com")
    .To("YourName@example.com")
    .Subject("Hello World")
    .Attach(@"C:\Temp\EmailClientSmtp.log")
    .SendAsync();
```

Using the method above will pass exceptions up to the next layer, but if you just want to log exceptions then continue with a "false" output that can also be done using the "try" prefix:

```csharp
bool isSent = await smtpSender.WriteEmail
    .From("me@example.com", "My Name")
    .To("you@example.com", "Your Name")
    .Cc("friend@example.com")
    .Bcc("admin@localhost")
    .Subject($"Hello at {DateTime.Now}!")
    .BodyText("Optional text/plain content.")
    .BodyHtml("Optional text/html content.<br/>")
    .TryAttach("C:/Temp/attachment1.txt", "C:/Temp/attachment2.pdf")
    .TrySendAsync();

_logger.LogInformation("Email {result}.", isSent ? "sent" : "failed to send");
```

Further examples (how to set up MailKit SMTP server logs etc.) can be found in the 'samples' and 'tests' folders on [GitHub](https://github.com/danzuep/MailKitSimplified).

### MailKitSimplified.Sender Dependency Injection

[Dependency Injection](https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection-usage#register-services-for-di) is recommended over manual setup as the built-in garbage collector will handle lifetime and disposal.

```csharp
using MailKitSimplified.Sender;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddHostedService<ExampleNamespace.Worker>();
        services.AddMailKitSimplifiedEmailSender(context.Configuration);
    })
    .Build();

await host.RunAsync();
```

You'll also need the following in appsettings.json:

```json
{
  "EmailSender:SmtpHost": "smtp.example.com",
}
```

Other optional settings include SmtpPort, ProtocolLog, SmtpCredential:UserName and SmtpCredential:Password.

Now you can use the fully configured ISmtpSender or IEmailWriter anywhere you want with no other setup! For example:

```csharp
public class EmailService
{
    private readonly IEmailWriter _writeEmail;

    public EmailService(IEmailWriter writeEmail) {
        _writeEmail = writeEmail;
    }
}
```

That's how sending an email can become as simple as one line of code.

```csharp
await _writeEmail.To("test@localhost").SendAsync();
```

## MailKitSimplified.Receiver Usage

### Setup

If you're not familiar with dependency injection then you can specify the IMAP host address like this:

```csharp
using var imapReceiver = ImapReceiver.Create("imap.example.com", 0, "U5ern@m3", "P455w0rd");
```

An email receiver must have a IMAP host address, a network credential (unless you're using something like [smtp4dev](https://github.com/rnwood/smtp4dev)), and sometimes a port number, but leaving the port as the default value of 0 will normally choose the right port automatically.

### Receiving Mail

```csharp
var mimeMessageQueue = await imapReceiver.ReadMail
    .GetMimeMessagesAsync();
```

```csharp
var mimeMessages = await imapReceiver.ReadFrom("INBOX/My Subfolder").Skip(0).Take(10)
    .GetMessageSummariesAsync(MessageSummaryItems.UniqueId);
```

Further examples (how to set up MailKit IMAP server logs etc.) can be found in the 'samples' and 'tests' folders on [GitHub](https://github.com/danzuep/MailKitSimplified).

### MailKitSimplified.Receiver Dependency Injection

[Dependency Injection](https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection-usage#register-services-for-di) is recommended over manual setup as the built-in garbage collector will handle lifetime and disposal.

```csharp
using MailKitSimplified.Receiver;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddHostedService<ExampleNamespace.EmailService>();
        services.AddMailKitSimplifiedEmailReceiver(context.Configuration);
    })
    .Build();

await host.RunAsync();
```

You'll also need the following in appsettings.json:

```json
{
  "EmailReceiver": {
    "MailFolderName": "INBOX",
    "ImapHost": "localhost",
    "ImapPort": 143,
    "ImapCredential": {
      "UserName": "",
      "Password": ""
    },
    "ProtocolLog": "C:\\Temp\\Email logs\\ImapClient.txt"
  },
}
```

Now you can use the fully configured IImapReceiver or IMailReader anywhere you want with no other setup! For example:

```csharp
public class EmailService
{
    private readonly IMailReader _readEmail;

    public EmailService(IMailReader readEmail) {
        _readEmail = readEmail;
    }
}
```

That's how receiving emails can become as simple as one line of code.

```csharp
var mimeMessages = await _readEmail.Skip(0).Take(10).GetMimeMessagesAsync();
```
