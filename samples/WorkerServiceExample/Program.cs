using MailKitSimplified.Sender.Extensions;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddHostedService<ExampleNamespace.Worker>();
        services.AddMailKitSimplifiedEmailSender(context.Configuration);
    })
    .Build();

await host.RunAsync();