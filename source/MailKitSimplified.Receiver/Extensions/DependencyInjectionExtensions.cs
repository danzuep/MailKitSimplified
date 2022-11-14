using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using MailKitSimplified.Receiver.Abstractions;
using MailKitSimplified.Receiver.Models;
using MailKitSimplified.Receiver.Services;
using MailKit;
using MailKit.Net.Imap;

namespace MailKitSimplified.Receiver.Extensions
{
    public static class DependencyInjectionExtensions
    {
        public static IServiceCollection AddMailKitSimplifiedEmailReceiver(this IServiceCollection services, IConfiguration configuration, string sectionName = EmailReceiverOptions.SectionName)
        {
            var configSection = configuration.GetRequiredSection(sectionName);
            // This adds IOptions<EmailReceiverOptions> from appsettings.json
            services.Configure<EmailReceiverOptions>(configSection);
            services.AddTransient<IImapClient, ImapClient>();
            services.AddTransient<IProtocolLogger, ProtocolLogger>();
            services.AddTransient<IMailFolderReader, MailFolderReader>();
            services.AddTransient<IImapReceiver, ImapReceiver>();
            return services;
        }
    }
}
