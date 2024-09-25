using MailKitSimplified.Receiver;
using MailKitSimplified.Sender;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EmailWpfApp.Extensions
{
    internal static class ServiceExtensions
    {
        internal static IServiceCollection ConfigureServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddScopedMailKitSimplifiedEmailSender(configuration);
            services.AddScopedMailKitSimplifiedEmailReceiver(configuration);
            return services;
        }
    }
}
