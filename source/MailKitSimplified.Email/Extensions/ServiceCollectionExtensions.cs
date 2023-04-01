using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MailKitSimplified.Sender;
using MailKitSimplified.Receiver;
using MailKitSimplified.Email.Models;

namespace MailKitSimplified.Email
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddMailKitSimplifiedEmail(this IServiceCollection services, IConfiguration configuration, string sectionName = EmailOptions.SectionName)
        {
            services.Configure<EmailOptions>(configuration.GetSection(sectionName));
            services.AddMailKitSimplifiedEmailSender(configuration);
            services.AddMailKitSimplifiedEmailReceiver(configuration);
            return services;
        }

    }
}
