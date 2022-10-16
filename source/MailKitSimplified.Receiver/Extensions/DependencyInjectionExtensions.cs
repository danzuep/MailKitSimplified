using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using MailKitSimplified.Receiver.Models;

namespace MailKitSimplified.Receiver.Extensions
{
    public static class DependencyInjectionExtensions
    {
        public static IServiceCollection AddMailKitSimplifiedEmailReceiver(this IServiceCollection services, IConfiguration configuration, string sectionName = EmailReceiverOptions.SectionName)
        {
            var configSection = configuration.GetRequiredSection(sectionName);
            // This adds IOptions<EmailSenderOptions> from appsettings.json
            services.Configure<EmailReceiverOptions>(configSection);
            //services.AddTransient<IFileHandler, FileHandler>();
            //services.AddTransient<IMimeAttachmentHandler, MimeAttachmentHandler>();
            //services.AddTransient<IEmail, Email>();
            //services.AddTransient<IEmailWriter, EmailWriter>();
            //services.AddTransient<IEmailSender, MimeMessageSender>();
            return services;
        }
    }
}
