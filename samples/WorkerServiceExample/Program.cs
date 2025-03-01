using MailKitSimplified.Sender;
using MailKitSimplified.Receiver;
using ExampleNamespace;

using var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddHostedService<Worker>();
        services.AddScopedMailKitSimplifiedEmailSender(context.Configuration);
        services.AddScopedMailKitSimplifiedEmailReceiver(context.Configuration);
        var workerSection = context.Configuration.GetRequiredSection(EmailWorkerOptions.SectionName);
        services.Configure<EmailWorkerOptions>(workerSection);
    })
    .ConfigureLogging(l => l.SetMinimumLevel(LogLevel.Trace))
    .Build();

await host.RunAsync();