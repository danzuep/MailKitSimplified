using MailKitSimplified.Sender.Abstractions;

namespace ExampleNamespace
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly ISmtpSender _smtpSender;

        public Worker(ILogger<Worker> logger, ISmtpSender emailSender)
        {
            _logger = logger;
            _smtpSender = emailSender;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken = default)
        {
            bool isSent = await _smtpSender.WriteEmail
                .From("me@localhost")
                .To($"{Guid.NewGuid():N}@localhost")
                .TrySendAsync(stoppingToken);
            _logger.LogInformation("Email {result}.", isSent ? "sent" : "failed to send");
        }
    }
}