using MailKitSimplified.Sender;
using MailKitSimplified.Receiver;
using MailKitSimplified.Sender.Abstractions;
using MailKitSimplified.Sender.Models;
using MailKitSimplified.Sender.Services;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddHostedService<ExampleNamespace.Worker>();
        var requiredSection = context.Configuration.GetRequiredSection(EmailSenderOptions.SectionName);
        services.Configure<EmailSenderOptions>(requiredSection);
        services.AddTransient<IEmailWriter, EmailWriter>();
        services.AddTransient<ISmtpSender, SmtpSender>();
        //services.AddMailKitSimplifiedEmailSender(context.Configuration);
        services.AddMailKitSimplifiedEmailReceiver(context.Configuration);
    })
    .Build();

await host.RunAsync();