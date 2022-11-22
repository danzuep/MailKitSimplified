using MailKit;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using MailKitSimplified.Receiver.Abstractions;
using MailKitSimplified.Receiver.Models;
using MailKitSimplified.Receiver.Services;

namespace MailKitSimplified.Receiver
{
    [ExcludeFromCodeCoverage]
    public static class DependencyInjectionExtensions
    {
        public static IServiceCollection AddMailKitSimplifiedEmailReceiver(this IServiceCollection services, IConfiguration configuration, string sectionName = EmailReceiverOptions.SectionName)
        {
            // This adds IOptions<EmailReceiverOptions> from appsettings.json
            var configSection = configuration.GetRequiredSection(sectionName);
            services.Configure<EmailReceiverOptions>(configSection);
            services.AddTransient<IMailReader, MailReader>();
            services.AddTransient<IMailFolderClient, MailFolderClient>();
            services.AddTransient<IMailFolderReader, MailFolderReader>();
            services.AddTransient<IProtocolLogger, MailKitProtocolLogger>();
            services.AddTransient<IImapReceiver, ImapReceiver>();
            return services;
        }
    }
}
