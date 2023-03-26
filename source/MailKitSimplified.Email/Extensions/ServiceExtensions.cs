using MailKitSimplified.Sender;
using MailKitSimplified.Receiver;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MailKitSimplified.Email.Models;

namespace MailKitSimplified.Email.Extensions
{
    internal static class ServiceExtensions
    {
        internal static IServiceCollection AddMailKitSimplifiedEmail(this IServiceCollection services, IConfiguration configuration, string sectionName = EmailOptions.SectionName)
        {
            var emailConfigurationSection = configuration.GetRequiredSection(sectionName);
            services.Configure<EmailOptions>(emailConfigurationSection);
            services.AddMailKitSimplifiedEmailSender(configuration);
            services.AddMailKitSimplifiedEmailReceiver(configuration);
            return services;
        }

    }
}
