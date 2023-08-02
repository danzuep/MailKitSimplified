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
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Add the MailKitSimplified.SmtpSender configuration and services.
        /// Adds IOptions<<see cref="EmailSenderOptions"/>>,
        /// <see cref="IEmailWriter"/>, and <see cref="ISmtpSender"/>.
        /// </summary>
        /// <param name="services">Collection of service descriptors.</param>
        /// <param name="configuration">Application configuration properties.</param>
        /// <param name="smtpSectionName">SMTP configuration section name.</param>
        /// <returns><see cref="IServiceCollection"/>.</returns>
        public static IServiceCollection AddMailKitSimplifiedEmailSender(this IServiceCollection services, IConfiguration configuration, string smtpSectionName = EmailSenderOptions.SectionName)
        {
            services.AddMailKitSimplifiedEmailSenderOptions(configuration, smtpSectionName);
            services.AddTransientMailKitSimplifiedEmailSender();
            return services;
        }

        /// <summary>
        /// Add the MailKitSimplified.SmtpSender configuration and services.
        /// Adds IOptions<<see cref="EmailSenderOptions"/>>,
        /// <see cref="IEmailWriter"/>, and <see cref="ISmtpSender"/>.
        /// </summary>
        /// <param name="services">Collection of service descriptors.</param>
        /// <param name="configuration">Application configuration properties.</param>
        /// <param name="smtpSectionName">SMTP configuration section name.</param>
        /// <returns><see cref="IServiceCollection"/>.</returns>
        public static IServiceCollection AddScopedMailKitSimplifiedEmailSender(this IServiceCollection services, IConfiguration configuration, string smtpSectionName = EmailSenderOptions.SectionName)
        {
            services.AddMailKitSimplifiedEmailSenderOptions(configuration, smtpSectionName);
            services.AddScopedMailKitSimplifiedEmailSender();
            return services;
        }

        /// <summary>
        /// Add the MailKitSimplified.SmtpSender configuration.
        /// Adds IOptions<<see cref="EmailSenderOptions"/>>.
        /// </summary>
        /// <param name="services">Collection of service descriptors.</param>
        /// <param name="configuration">Application configuration properties.</param>
        /// <param name="smtpSectionName">SMTP configuration section name.</param>
        /// <returns><see cref="IServiceCollection"/>.</returns>
        private static IServiceCollection AddMailKitSimplifiedEmailSenderOptions(this IServiceCollection services, IConfiguration configuration, string smtpSectionName = EmailSenderOptions.SectionName)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));
            var smtpSection = configuration.GetRequiredSection(smtpSectionName);
            services.Configure<EmailSenderOptions>(smtpSection);
            var writerSection = smtpSection.GetSection(EmailWriterOptions.SectionName);
            services.Configure<EmailWriterOptions>(writerSection);
            return services;
        }

        /// <summary>
        /// Add the MailKitSimplified.SmtpSender services.
        /// Adds <see cref="IEmailWriter"/>, and <see cref="ISmtpSender"/>.
        /// </summary>
        /// <param name="services">Collection of service descriptors.</param>
        /// <returns><see cref="IServiceCollection"/>.</returns>
        private static IServiceCollection AddScopedMailKitSimplifiedEmailSender(this IServiceCollection services)
        {
            //services.AddScoped<IProtocolLogger, NullProtocolLogger>();
            services.AddSingleton<IFileSystem, FileSystem>();
            services.AddScoped<IEmailWriter, EmailWriter>();
            services.AddScoped<ISmtpSender, SmtpSender>();
            return services;
        }

        /// <summary>
        /// Add the MailKitSimplified.SmtpSender services.
        /// Adds <see cref="IEmailWriter"/>, and <see cref="ISmtpSender"/>.
        /// </summary>
        /// <param name="services">Collection of service descriptors.</param>
        /// <returns><see cref="IServiceCollection"/>.</returns>
        private static IServiceCollection AddTransientMailKitSimplifiedEmailSender(this IServiceCollection services)
        {
            //services.AddTransient<IProtocolLogger, NullProtocolLogger>();
            services.AddSingleton<IFileSystem, FileSystem>();
            services.AddTransient<IEmailWriter, EmailWriter>();
            services.AddTransient<ISmtpSender, SmtpSender>();
            return services;
        }

        /// <summary>
        /// Add the MailKitSimplified.SmtpSender configuration and services.
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

        /// <summary>
        /// Add the MailKitSimplified.EmailWriter configuration and services.
        /// Adds IOptions<<see cref="EmailWriterOptions"/>>.
        /// </summary>
        /// <param name="services">Collection of service descriptors.</param>
        /// <param name="emailWriterOptions">Email writer options.</param>
        /// <returns><see cref="IServiceCollection"/>.</returns>
        public static IServiceCollection AddMailKitSimplifiedEmailWriter(this IServiceCollection services, Action<EmailWriterOptions> emailWriterOptions)
        {
            if (emailWriterOptions == null)
                throw new ArgumentNullException(nameof(emailWriterOptions));
            services.Configure(emailWriterOptions);
            return services;
        }
    }
}
