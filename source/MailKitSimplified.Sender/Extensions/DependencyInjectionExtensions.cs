using System.IO.Abstractions;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using MailKitSimplified.Sender.Abstractions;
using MailKitSimplified.Sender.Services;
using MailKitSimplified.Sender.Models;

namespace MailKitSimplified.Sender
{
    [ExcludeFromCodeCoverage]
    public static class DependencyInjectionExtensions
    {
        public static IServiceCollection AddMailKitSimplifiedEmailSender(this IServiceCollection services, IConfiguration configuration, string sectionName = EmailSenderOptions.SectionName)
        {
            // This adds IOptions<EmailSenderOptions> from appsettings.json
            var configSection = configuration.GetRequiredSection(sectionName);
            services.Configure<EmailSenderOptions>(configSection);
            services.AddTransient<IFileSystem, FileSystem>();
            services.AddTransient<IEmailWriter, EmailWriter>();
            services.AddTransient<ISmtpSender, SmtpSender>();
            return services;
        }
    }
}
