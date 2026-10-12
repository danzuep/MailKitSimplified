## Sender

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

You could choose to configure the services manually, but I think appsettings.json is better:

```csharp
services.AddEmailSenderOptions(o => { o.SmtpHost = "localhost"; o.SmtpPort = 25; });
```

Add the following in appsettings.json (check Properties -> Copy to Output Directory -> Copy if newer):

```json
{
  "EmailSender": {
    "SmtpHost": "smtp.example.com",
    "SmtpPort": 587,
    "SmtpCredential": {
      "UserName": "",
      "Password": ""
    },
    "ProtocolLog": "Logs\\SmtpClient.txt"
  },
}
```

The only required setting is SmtpHost, so you could just do: `"EmailSender:SmtpHost": "smtp.example.com",` instead.

Now you can use the fully configured ISmtpSender or IEmailWriter anywhere you want with no other setup! For example:

```csharp
public class EmailService
{
    private readonly IEmailWriter _writeEmail;
    public EmailService(IEmailWriter writeEmail) =>
        _writeEmail = writeEmail;
}
```

That's how sending an email can become as simple as one line of code.

```csharp
await _writeEmail.To("test@localhost").SendAsync();
```

### Manual Setup

This is not recommended, but if you wanted to manually add configuration settings with Microsoft.Extensions.Configuration.Json and Microsoft.Extensions.Logging.Console/Debug you could do this:

```csharp
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using MailKitSimplified.Sender.Models;
using MailKitSimplified.Sender.Services;

using var loggerFactory = LoggerFactory.Create(_ => _.SetMinimumLevel(LogLevel.Trace).AddDebug().AddConsole());
var configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
var emailSenderOptions = configuration.GetRequiredSection(EmailSenderOptions.SectionName).Get<EmailSenderOptions>();
using var smtpSender = SmtpSender.Create(emailSenderOptions).SetLogger(loggerFactory);
```

## Receiver

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

You could choose to configure the services manually, but I think appsettings.json is better:

```csharp
services.AddEmailReceiverOptions(o => { o.ImapHost = "localhost"; o.ImapPort = 143; });
services.AddFolderMonitorOptions(o => { o.IdleMinutes = 9; });
```

Add the following in appsettings.json (check Properties -> Copy to Output Directory -> Copy if newer):

```json
{
  "EmailReceiver": {
    "ImapHost": "imap.example.com",
    "ImapPort": 993,
    "MailFolderName": "INBOX",
    //"MailFolderAccess": "ReadOnly",
    //"CapabilitiesToRemove": "None",
    //"SocketOptions": "None",
    //"Timeout": "00:01:00",
    "ImapCredential": {
      "UserName": "user@example.com",
      "Password": "App1icati0nP455w0rd"
    },
    "ProtocolLog": "Logs\\ImapClient.txt"
  },
  "FolderMonitor": {
    "IgnoreExistingMailOnConnect": false,
    "MessageSummaryItems": "None",
    "IdleMinutes": 9,
    "MaxRetries": 3
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "MailKitSimplified.Receiver.Services.MailKitProtocolLogger": "Debug"
    }
  }
}
```

Now you can use the fully configured IImapReceiver, IMailFolderReader, or IMailReader anywhere you want with no other setup! For example:

```csharp
public class EmailService
{
    private readonly IMailFolderReader _readMail;
    public EmailService(IMailFolderReader readMail) =>
        _readMail = readMail;
}
```

That's how receiving emails can become as simple as one line of code.

```csharp
var mimeMessages = await _readMail.Take(10).GetMimeMessagesAsync();
```

### Manual Setup

This is not recommended, but if you wanted to manually add configuration settings with Microsoft.Extensions.Configuration.Json and Microsoft.Extensions.Logging.Console/Debug you could do this:

```csharp
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using MailKitSimplified.Receiver.Models;
using MailKitSimplified.Receiver.Services;

using var loggerFactory = LoggerFactory.Create(_ => _.SetMinimumLevel(LogLevel.Trace).AddDebug().AddConsole());
var configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
var emailReceiverOptions = configuration.GetRequiredSection(EmailReceiverOptions.SectionName).Get<EmailReceiverOptions>();
using var imapReceiver = ImapReceiver.Create(emailReceiverOptions).SetLogger(loggerFactory);
```

## ImapReceiverFactory

Assuming you've followed the Receiver instructions above, add the following in appsettings.json:

```json
{
  "Mailbox": {
    "EmailReceivers": [
      {
        "MailFolderNames": [ "INBOX" ],
        "ImapHost": "localhost"
      }
    ]
  }
}
```

## MailFolderMonitorFactory

Assuming you've followed the Receiver instructions above, add the following in appsettings.json:

```json
{
  "Mailbox": {
    "FolderMonitors": [
      {
        "EmailReceiver": {
          "MailFolderName": "INBOX",
          "ImapHost": "localhost"
        },
        "IgnoreExistingMailOnConnect": false
      }
    ]
  }
}
```
