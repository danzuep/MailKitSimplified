using EmailWpfApp.Data;
using MailKitSimplified.Receiver;
using MailKitSimplified.Sender;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EmailWpfApp.Extensions
{
    internal static class ServiceExtensions
    {
        internal static IServiceCollection ConfigureServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddLogging(options => options.AddConfiguration(configuration));
            services.AddDbContext<EmailDbContext>(options => options.UseSqlite("Data Source=Email.db"));
            services.AddMailKitSimplifiedEmailSender(configuration);
            services.AddMailKitSimplifiedEmailReceiver(configuration);
            return services;
        }
    }
}
