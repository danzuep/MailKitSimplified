using MailKitSimplified.Sender;
using MailKitSimplified.Receiver;
using ExampleNamespace;

using var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddHostedService<Worker>();
        //services.AddMailKitSimplifiedEmail(context.Configuration);
        services.AddScopedMailKitSimplifiedEmailSender(context.Configuration);
        services.AddScopedMailKitSimplifiedEmailReceiver(context.Configuration);
        var workerSection = context.Configuration.GetRequiredSection(EmailWorkerOptions.SectionName);
        services.Configure<EmailWorkerOptions>(workerSection);
    })
    .Build();

await host.RunAsync();