using MailKitSimplified.Core.Abstractions;

namespace WorkerServiceExample
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IEmailSender _smtpSender;

        public Worker(ILogger<Worker> logger, IEmailSender emailSender)
        {
            _logger = logger;
            _smtpSender = emailSender;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            bool isSent = await _smtpSender.WriteEmail
                .From("me@example.com")
                .To("you@example.com")
                .TrySendAsync();
            _logger.LogInformation("Email {result}.", isSent ? "sent" : "failed to send");
        }
    }
}