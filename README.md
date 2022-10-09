# MailKitSimplified.Sender ![Build and test workflow result badge](https://github.com/danzuep/MailKitSimplified.Sender/workflows/Pipeline/badge.svg)

When I first started using MailKit I was surprised at how many steps were involved in getting it all set up and working. The aim of this package is to make sending an email as simple as possible.

## Usage

### Dependency Injection

This is recommended over manual setup as the build-in garbage collector will handle lifetime and disposal.
```
public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
{
    services.Configure<EmailSenderOptions>(configuration.GetRequiredSection(EmailSenderOptions.SectionName).Get<EmailSenderOptions>()); // this adds IOptions<EmailSenderOptions> from appsettings.json
    services.AddScopedService<IEmail, Email>();
    services.AddScopedService<IEmailWriter, EmailWriter>();
    services.AddScopedService<IEmailSender, MimeMessageSender>();
}
```
This can then be used as follows:
```
public class EmailService {

    private readonly IEmailSender _smtpSender;

    public EmailService(IEmailSender smtpSender) {
        _smtpSender = smtpSender;
    }

    public async Task SendEmailAsync() {
        await _smtpSender.WriteEmail
            .From("me@example.com")
            .To("you@example.com")
            .Subject("Hi")
            .Body("~")
            .Attach("C:/Temp/attachment1.txt", "C:/Temp/attachment2.pdf")
            .SendAsync();
    }
}
```

### Manual Setup

Either dependency injection (`IEmailSender smtpSender`) or:
```
using var smtpSender = MimeMessageSender.Create("smtp.example.com");
```

### Sending Mail

```
var email = smtpSender.WriteEmail
    .From("me@example.com")
    .To("you@example.com")
    .Subject("Hi")
    .Body("~")
    .Attach("C:/Temp/attachment1.txt", "C:/Temp/attachment2.pdf");

await email.SendAsync();
```

or

```
bool isSent = await smtpSender.WriteEmail
    .From("me@example.com")
    .To("you@example.com")
    .Subject("Hi")
    .Body("~")
    .SendAsync();
```

Further examples (MailKit log output etc.) can be found in MailKitSimplifiedSenderUnitTests.

### Receiving Mail

Coming in the near future.
