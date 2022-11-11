# MailKitSimplified [![Development](https://github.com/danzuep/MailKitSimplified/actions/workflows/development.yml/badge.svg)](https://github.com/danzuep/MailKitSimplified/actions/workflows/development.yml) [![Release](https://github.com/danzuep/MailKitSimplified/actions/workflows/release.yml/badge.svg)](https://github.com/danzuep/MailKitSimplified/actions/workflows/release.yml)

Sending and receiving emails sounds simple, after all, electronic mail existed [decades](https://en.wikipedia.org/wiki/History_of_email) before the [Internet](https://en.wikipedia.org/wiki/History_of_the_Internet). If you're looking for a an all-in-one .NET solution for email, you'll quickly discover [MailKit](https://github.com/jstedfast/MailKit) is recommended by even the likes of [Microsoft](https://learn.microsoft.com/en-us/dotnet/api/system.net.mail.smtpclient?view=net-6.0#remarks) due to how it implements the [RFC standard](https://www.rfc-editor.org/rfc/rfc2822). Unfortunately the downside of doing it all is that MailKit can be difficult to [set up](https://github.com/jstedfast/MailKit#using-mailkit) [and use](https://github.com/jstedfast/MimeKit/blob/master/FAQ.md#messages-1), especially the first time you go to try something like [checking attachments](https://github.com/jstedfast/MimeKit/blob/master/FAQ.md#q-how-do-i-tell-if-a-message-has-attachments) or [writing a reply](https://github.com/jstedfast/MimeKit/blob/master/FAQ.md#q-how-do-i-reply-to-a-message). The aim of this package is to make sending (and receiving) emails as simple as possible!

Sending an email is now as easy as:
```csharp
await writeEmail.To("test@localhost").SendAsync();
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
var email = smtpSender.WriteEmail
    .From("me@example.com")
    .To("you@example.com")
    .Subject("Hi")
    .Body("~");

await email.SendAsync();
```

Any configuration issues will throw an exception, but you can also opt to just log any exceptions and continue with a `false` output:

```csharp
bool isSent = await smtpSender.WriteEmail
    .From("me@example.com", "My Name")
    .To("you@example.com", "Your Name")
    .Cc("friend1@example.com")
    .Bcc("friend2@example.com")
    .Subject("Hey You")
    .Body($"Hello at {DateTime.Now}.")
    .Attach("C:/Temp/attachment1.txt", "C:/Temp/attachment2.pdf")
    .Attach("./attachment3.docx")
    .TrySendAsync();

_logger.LogInformation("Email {result}.", isSent ? "sent" : "failed to send");
```

Further examples (detailed MailKit SMPT server logs etc.) can be found in MailKitSimplifiedSenderUnitTests and the example solution file.

### Dependency Injection

This is recommended over manual setup as the built-in garbage collector will handle lifetime and disposal.

```csharp
using MailKitSimplified.Sender.Extensions;

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
  "EmailSender:SmtpHost": "smtp.example.com"
}
```

Other optional settings include SmtpPort, ProtocolLog, SmtpCredential:UserName and SmtpCredential:Password.

Now you can use the fully configured IEmailSender or IEmailWriter anywhere you want with no other setup! For example:

```csharp
public class EmailService
{
    private readonly IEmailWriter _writeEmail;

    public EmailService(IEmailWriter emailWriter) {
        _writeEmail = emailWriter;
    }
}
```

That's how sending an email can become as simple as one line of code.

```csharp
await _writeEmail.To("test@localhost").SendAsync();
```

## MailKitSimplified.Receiver Usage

### Setup

If you're not familiar with dependency injection then just use this:

```csharp
using var imapReceiver = ImapReceiver.Create("imap.example.com", 0, "U5ern@me", "P@55w0rd");
```

An email receiver must have a IMAP host address, a network credential (unless you're using something like `smtp4dev`), and sometimes a port number, but leaving the port as the default value of 0 will normally choose the right port automatically.

### Receiving Mail

```csharp
var mailboxReceiver = imapReceiver
    .ReadFrom("INBOX")
    .Skip(0)
    .Take(10);

var mimeMessages = await mailboxReceiver.GetMimeMessagesAsync();
```

```csharp
var mimeMessageQueue = await imapReceiver
    .ReadFrom("INBOX")
    .GetMimeMessagesAsync();
```

Further examples (detailed MailKit IMAP server logs etc.) can be found in the 'samples' and 'tests' folders.
