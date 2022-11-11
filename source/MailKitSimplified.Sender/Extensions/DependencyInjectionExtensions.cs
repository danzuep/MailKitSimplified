using MailKitSimplified.Core.Abstractions;
using MailKitSimplified.Core.Models;
using MailKitSimplified.Core.Services;
using MailKitSimplified.Sender.Abstractions;
using MailKitSimplified.Sender.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using System.IO.Abstractions;
using MailKit.Net.Smtp;
using MailKitSimplified.Sender.Models;

namespace MailKitSimplified.Sender.Extensions
{
    public static class DependencyInjectionExtensions
    {
        public static IServiceCollection AddMailKitSimplifiedEmailSender(this IServiceCollection services, IConfiguration configuration, string sectionName = EmailSenderOptions.SectionName)
        {
            // This adds IOptions<EmailSenderOptions> from appsettings.json
            var configSection = configuration.GetRequiredSection(sectionName);
            services.Configure<EmailSenderOptions>(configSection);
            services.AddTransient<IFileSystem, FileSystem>();
            services.AddTransient<IAttachmentHandler, AttachmentHandler>();
            services.AddTransient<ISendableEmail, SendableEmail>();
            services.AddTransient<ISendableEmailWriter, SendableEmailWriter>();
            services.AddTransient<ISmtpSender, SmtpSender>();
            return services;
        }
    }
}
