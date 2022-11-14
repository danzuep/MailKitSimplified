using System.IO.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using MailKit;
using MailKit.Net.Smtp;
using MailKitSimplified.Sender.Abstractions;
using MailKitSimplified.Sender.Services;
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
            services.AddTransient<ISmtpClient, SmtpClient>();
            services.AddTransient<IProtocolLogger, ProtocolLogger>();
            services.AddTransient<IAttachmentHandler, AttachmentHandler>();
            services.AddTransient<IEmailWriter, EmailWriter>();
            services.AddTransient<ISmtpSender, SmtpSender>();
            return services;
        }
    }
}
