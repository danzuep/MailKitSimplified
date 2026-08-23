#:package Testcontainers@4.14.0
#:package MailKitSimplified.Sender@2.14.0
#:package MailKitSimplified.Receiver@2.14.0
#:property PublishTrimmed=false

using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using MailKitSimplified.Sender.Services;
using MailKitSimplified.Receiver.Services;

await using var smtp4Dev = new Smtp4DevFixture();
Console.WriteLine("Starting smtp4dev with Testcontainers...");
await smtp4Dev.StartAsync();

Console.WriteLine("Initializing smtp4dev with a test email...");
using var smtpSender = SmtpSender.Create(smtp4Dev.SmtpHost);
await smtpSender.WriteEmail
    .From("sender@example.test")
    .To("recipient@example.test")
    .Subject("E2E smtp4dev smoke test")
    .BodyText("This email was sent by the .NET end-to-end smoke test.")
    .SendAsync();
Console.WriteLine("Sent smoke-test message to smtp4dev.");

var cancellationTokenSource = new CancellationTokenSource();
var cancellationToken = cancellationTokenSource.Token;

Console.WriteLine("Initializing IMAP listener...");
using var imapReceiver = ImapReceiver.Create(smtp4Dev.ImapHost);
await imapReceiver.MonitorFolder.OnMessageArrival(_ => {
        Console.WriteLine("Test email arrived, smtp4dev is healthy.");
        cancellationTokenSource.Cancel();
    }).IdleAsync(cancellationToken);

sealed class Smtp4DevFixture : IAsyncDisposable
{
    private readonly IContainer container;

    public Smtp4DevFixture()
    {
        container = new ContainerBuilder("rnwood/smtp4dev:v3")
            .WithPortBinding(80, true)
            .WithPortBinding(25, true)
            .WithPortBinding(143, true)
            .WithEnvironment("ServerOptions__Urls", "http://*:80")
            .WithEnvironment("ServerOptions__HostName", "smtp4dev")
            .WithWaitStrategy(Wait.ForUnixContainer()
                //.UntilHttpRequestIsSucceeded(request => request.ForPort(80))
                .UntilInternalTcpPortIsAvailable(25)
                .UntilInternalTcpPortIsAvailable(143))
            .Build();
    }

    public int SmtpPort => container.GetMappedPublicPort(25);
    public int ImapPort => container.GetMappedPublicPort(143);
    public string SmtpHost => $"localhost:{SmtpPort}";
    public string ImapHost => $"localhost:{ImapPort}";

    public Task StartAsync() => container.StartAsync();

    public async ValueTask DisposeAsync()
    {
        await container.DisposeAsync();
    }
}