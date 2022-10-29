# MailKitSimplified [![Build and Test results summary](https://github.com/danzuep/MailKitSimplified/actions/workflows/development.yml/badge.svg)](https://github.com/danzuep/MailKitSimplified/actions/workflows/development.yml)

Sending and receiving emails sounds simple, after all, electronic mail existed [decades](https://en.wikipedia.org/wiki/History_of_email) before the [Internet](https://en.wikipedia.org/wiki/History_of_the_Internet). If you're looking for a an all-in-one .NET solution for email, you'll quickly discover [MailKit](https://github.com/jstedfast/MailKit) is recommended by even the likes of [Microsoft](https://learn.microsoft.com/en-us/dotnet/api/system.net.mail.smtpclient?view=net-6.0#remarks). Unfortunately for new users though, MailKit can do too much, so when I first started using it I was surprised at how many configuration steps were involved in getting it set up, and on the receiving end how poorly some real-world SMTP servers out there implement [the standard](https://www.rfc-editor.org/rfc/rfc2822). The aim of this package is to make sending an email as simple as possible.

## Sender Usage

### Setup

If you're not familiar with dependency injection then just use this:
```
using var smtpSender = MimeMessageSender.Create("smtp.example.com");
```

### Sending Mail

```
var email = smtpSender.WriteEmail
    .From("me@example.com")
    .To("you@example.com")
    .Subject("Hi")
    .Body("~");

await email.SendAsync();
```

An email must have a SMTP host address, a `From` and at least one `To` address; that's all. You can use each method as many times as you want, but setting a subject or body multiple times will overwrite previous subject or body values. The order of anything after WriteEmail does not matter.
Any issues will throw an exception, but you can also opt to just log them and continue with a `false` output:

```
bool isSent = await smtpSender.WriteEmail
    .From("me@example.com", "My Name")
    .To("you@example.com", "Your Name")
    .To("friend1@example.com")
    .To("friend2@example.com")
    .Subject("Hey You")
    .Body($"Hello! {DateTime.Now}")
    .Attach("C:/Temp/attachment1.txt", "C:/Temp/attachment2.pdf")
    .Attach("./attachment3.docx")
    .TrySendAsync();

_logger.LogInformation("Email {result}.", isSent ? "sent" : "failed to send");
```

Further examples (detailed MailKit SMPT server logs etc.) can be found in MailKitSimplifiedSenderUnitTests and the example solution file.

### Dependency Injection

This is recommended over manual setup as the built-in garbage collector will handle lifetime and disposal.
```
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
```
{
  "EmailSender:SmtpHost": "smtp.example.com"
}
```

Other optional settings include SmtpPort, ProtocolLog, SmtpCredential:UserName and SmtpCredential:Password.

Now you can use the fully configured IEmailSender anywhere you want with no other setup! For example:

```
public class EmailService {

    private readonly IEmailSender _smtpSender;

    public EmailService(IEmailSender smtpSender) {
        _smtpSender = smtpSender;
    }
}
```

Or, even easier, just start with IEmailWriter:

```
public class EmailService {

    private readonly IEmailWriter _writeEmail;

    public EmailService(IEmailWriter emailWriter) {
        _writeEmail = emailWriter;
    }

    public async Task SendTestEmailAsync(string bodyText = "") {
        await _writeEmail
            .From("me@example.com")
            .To("you@example.com")
            .Subject("Test email")
            .Body(bodyText)
            .SendAsync();
    }
}
```

## Receiver Usage

### Setup

If you're not familiar with dependency injection then just use this:
```
using var imapReceiver = MimeMessageReceiver.Create("imap.example.com", 0, "U5ern@me", "P@55w0rd");
```

An email receiver must have a IMAP host address, a network credential, and a valid mail folder to read from.

### Receiving Mail

```
var receiver = imapReceiver
    .ReadFrom("INBOX")
    .Skip(0)
    .Take(10);

await receiver.GetMimeMessagesAsync();
```

```
var mimeMessageQueue = await imapReceiver
    .ReadFrom("INBOX")
    .GetMimeMessagesAsync();
```

Further examples (detailed MailKit IMAP server logs etc.) can be found in MailKitSimplifiedReceiverUnitTests and the example solution file.
