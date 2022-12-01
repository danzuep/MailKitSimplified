using System;
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
        /// <summary>
        /// Add the MailKitSimplified.Sender configuration and services.
        /// Adds IOptions<<see cref="EmailSenderOptions"/>>,
        /// <see cref="IEmailWriter"/>, and <see cref="ISmtpSender"/>.
        /// </summary>
        /// <param name="services">Collection of service descriptors.</param>
        /// <param name="configuration">Application configuration properties.</param>
        /// <param name="sectionName">Configuration section name.</param>
        /// <returns><see cref="IServiceCollection"/>.</returns>
        public static IServiceCollection AddMailKitSimplifiedEmailSender(this IServiceCollection services, IConfiguration configuration, string sectionName = EmailSenderOptions.SectionName)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));
            var configSection = configuration.GetRequiredSection(sectionName);
            services.Configure<EmailSenderOptions>(configSection);
            services.AddMailKitSimplifiedEmailSender();
            return services;
        }

        /// <summary>
        /// Add the MailKitSimplified.Sender services.
        /// Adds <see cref="IEmailWriter"/>, and <see cref="ISmtpSender"/>.
        /// </summary>
        /// <param name="services">Collection of service descriptors.</param>
        /// <returns><see cref="IServiceCollection"/>.</returns>
        private static IServiceCollection AddMailKitSimplifiedEmailSender(this IServiceCollection services)
        {
            services.AddSingleton<IFileSystem, FileSystem>();
            services.AddTransient<IEmailWriter, EmailWriter>();
            services.AddTransient<ISmtpSender, SmtpSender>();
            return services;
        }

        /// <summary>
        /// Add the MailKitSimplified.Sender configuration and services.
        /// Adds IOptions<<see cref="EmailSenderOptions"/>>.
        /// </summary>
        /// <param name="services">Collection of service descriptors.</param>
        /// <param name="emailSenderOptions">Email sender options.</param>
        /// <returns><see cref="IServiceCollection"/>.</returns>
        public static IServiceCollection AddMailKitSimplifiedEmailSender(this IServiceCollection services, Action<EmailSenderOptions> emailSenderOptions)
        {
            if (emailSenderOptions == null)
                throw new ArgumentNullException(nameof(emailSenderOptions));
            services.Configure(emailSenderOptions);
            return services;
        }
    }
}
