using MailKitSimplified.Sender;
using MailKitSimplified.Receiver;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddHostedService<ExampleNamespace.Worker>();
        //services.AddMailKitSimplifiedEmail(context.Configuration);
        services.AddScopedMailKitSimplifiedEmailSender(context.Configuration);
        services.AddScopedMailKitSimplifiedEmailReceiver(context.Configuration);
    })
    .Build();

await host.RunAsync();