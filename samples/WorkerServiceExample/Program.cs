using MailKitSimplified.Sender;
using MailKitSimplified.Receiver;
using MailKitSimplified.Sender.Models;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddHostedService<ExampleNamespace.Worker>();
        //services.AddMailKitSimplifiedEmail(context.Configuration);
        services.AddScopedMailKitSimplifiedEmailSender(context.Configuration);
        services.AddScopedMailKitSimplifiedEmailReceiver(context.Configuration);
        var workerSection = context.Configuration.GetRequiredSection(EmailWorkerOptions.SectionName);
        services.Configure<EmailWorkerOptions>(workerSection);
    })
    .Build();

await host.RunAsync();