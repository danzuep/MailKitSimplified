using MailKitSimplified.Core.Abstractions;
using MailKitSimplified.Core.Models;
using MailKitSimplified.Core.Services;
using MailKitSimplified.Sender.Abstractions;
using MailKitSimplified.Sender.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace MailKitSimplified.Sender.Extensions
{
    public static class DependencyInjectionExtensions
    {
        public static IServiceCollection AddMailKitSimplifiedEmailSender(this IServiceCollection services, IConfiguration configuration, string sectionName = EmailSenderOptions.SectionName)
        {
            var configSection = configuration.GetRequiredSection(sectionName);
            // This adds IOptions<EmailSenderOptions> from appsettings.json
            services.Configure<EmailSenderOptions>(configSection);
            services.AddTransient<IFileHandler, FileHandler>();
            services.AddTransient<IMimeAttachmentHandler, MimeAttachmentHandler>();
            services.AddTransient<ISendableEmail, Email>();
            services.AddTransient<IEmailWriter, EmailWriter>();
            services.AddTransient<IEmailSender, SmtpSender>();
            return services;
        }
    }
}
