namespace ExporterExample.Abstractions
{
    public interface IEmailService
    {
        Task ReceiveTestAsync(CancellationToken cancellationToken = default);
        Task SendTestAsync(CancellationToken cancellationToken = default);
    }
}