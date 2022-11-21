using MailKitSimplified.Sender;
using MailKitSimplified.Receiver;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddHostedService<ExampleNamespace.Worker>();
        services.AddMailKitSimplifiedEmailSender(context.Configuration);
        services.AddMailKitSimplifiedEmailReceiver(context.Configuration);
    })
    .Build();

await host.RunAsync();