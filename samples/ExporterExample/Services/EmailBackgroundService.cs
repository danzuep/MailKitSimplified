using ExporterExample.Abstractions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ExampleNamespace;

public class EmailBackgroundService : BackgroundService
{
    private readonly IEmailService _emailService;
    private readonly ILogger<EmailBackgroundService> _logger;
    private readonly TimeSpan _sendInterval = TimeSpan.FromMinutes(5);
    private readonly TimeSpan _receiveInterval = TimeSpan.FromMinutes(3);

    public EmailBackgroundService(IEmailService emailService, ILogger<EmailBackgroundService> logger)
    {
        _emailService = emailService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Email background service starting.");

        // Start both send and receive loops concurrently
        var sendTask = RunSendLoopAsync(stoppingToken);
        var receiveTask = RunReceiveLoopAsync(stoppingToken);

        await Task.WhenAll(sendTask, receiveTask);

        _logger.LogInformation("Email background service stopping.");
    }

    private async Task RunSendLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogDebug("Triggering send test.");
            try
            {
                await _emailService.SendTestAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Send test failed.");
            }

            await Task.Delay(_sendInterval, cancellationToken);
        }
    }

    private async Task RunReceiveLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogDebug("Triggering receive test.");
            try
            {
                await _emailService.ReceiveTestAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Receive test failed.");
            }

            await Task.Delay(_receiveInterval, cancellationToken);
        }
    }
}