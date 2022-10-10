# MailKitSimplified.Sender ![Build and test workflow result badge](https://github.com/danzuep/MailKitSimplified.Sender/workflows/Build%20and%20Test/badge.svg)

Sending and receiving emails sounds simple, after all, electronic mail existed [decades](https://en.wikipedia.org/wiki/History_of_email) before the [Internet](https://en.wikipedia.org/wiki/History_of_the_Internet). If you're looking for a an all-in-one .NET solution for email, you'll quickly discover [MailKit](https://github.com/jstedfast/MailKit) is recommended by even the likes [Microsoft](https://learn.microsoft.com/en-us/dotnet/api/system.net.mail.smtpclient?view=net-6.0#remarks). Unfortunately for new users, MailKit can do too much though, so when I first started using it I was surprised at how many steps were involved in getting it all configured correctly and how poorly some real-world SMTP servers out there implement [the standard](https://www.rfc-editor.org/rfc/rfc2822). The aim of this package is to make sending an email as simple as possible.

## Usage

### Setup

If you're not sure what dependency injection is then just use this:
```
using var smtpSender = MimeMessageSender.Create("smtp.example.com");
```

### Sending Mail

```
var email = smtpSender.WriteEmail
    .From("me@example.com", "My Name")
    .To("you@example.com", "Your Name")
    .To("friend1@example.com")
    .To("friend2@example.com")
    .Subject("Hey You")
    .Body("Hello World")
    .Attach("C:/Temp/attachment1.txt", "C:/Temp/attachment2.pdf")
    .Attach("./attachment3.docx");

await email.SendAsync();
```

An email must have a `From` and at least one `To` address, order does not matter.
Setting a subject or body will overwrite previous ones to make things simpler.
Any issues will throw an exception, but you can also opt to just log them and continue with a `false` output:

```
bool isSent = await smtpSender.WriteEmail
    .From("me@example.com")
    .To("you@example.com")
    .TrySendAsync();

_logger.LogInformation("Email {result}.", isSent ? "sent" : "failed to send");
```

Further examples (detailed MailKit SMPT server logs etc.) can be found in MailKitSimplifiedSenderUnitTests and the example solution file.

### Dependency Injection

This is recommended over manual setup as the built-in garbage collector will handle lifetime and disposal.
```
public class Program
{
    public static async Task Main(string[] args)
    {
        IHost host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                services.AddHostedService<Worker>();
                ConfigureServices(services, context.Configuration);
            })
            .Build();

        await host.RunAsync();
    }

    public static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // This adds IOptions<EmailSenderOptions> from appsettings.json
        services.Configure<EmailSenderOptions>(configuration
            .GetRequiredSection(EmailSenderOptions.SectionName));
        services.AddScoped<IEmail, Email>();
        services.AddScoped<IEmailWriter, EmailWriter>();
        services.AddScoped<IEmailSender, MimeMessageSender>();
    }
}
```
This can then be referenced with no other setup in your service as follows:
```
public class EmailService {

    private readonly IEmailSender _smtpSender;

    public EmailService(IEmailSender smtpSender) {
        _smtpSender = smtpSender;
    }
}
```

### Receiving Mail

Coming in a sister package in the near future.
