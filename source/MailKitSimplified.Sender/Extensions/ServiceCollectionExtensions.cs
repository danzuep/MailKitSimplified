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
            services.AddMailKitSimplifiedEmailSender(ServiceLifetime.Transient);
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
            services.AddMailKitSimplifiedEmailSender(ServiceLifetime.Scoped);
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
        /// Adds a service of the lifetime and type specified in TService and an implementation specified
        /// in TImplementation to the specified Microsoft.Extensions.DependencyInjection.IServiceCollection.
        /// </summary>
        /// <typeparam name="TService">The type of the service to add.</typeparam>
        /// <typeparam name="TImplementation">The implementation of the service to add.</typeparam>
        /// <param name="services">The Microsoft.Extensions.DependencyInjection.IServiceCollection to add the service to.</param>
        /// <param name="serviceLifetime">The lifetime of the service to be added.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        private static IServiceCollection AddWithLifetime<TService, TImplementation>(this IServiceCollection services, ServiceLifetime serviceLifetime)
            where TService : class
            where TImplementation : class, TService
        {
            var descriptor = new ServiceDescriptor(typeof(TService), typeof(TImplementation), serviceLifetime);
            services.Add(descriptor);
            return services;
        }

        /// <summary>
        /// Add the MailKitSimplified.SmtpSender services with the specified lifetimes.
        /// Adds <see cref="IEmailWriter"/> and <see cref="ISmtpSender"/>.
        /// </summary>
        /// <inheritdoc cref="AddWithLifetime"/>
        private static IServiceCollection AddMailKitSimplifiedEmailSender(this IServiceCollection services, ServiceLifetime serviceLifetime)
        {
            services.AddSingleton<IFileSystem, FileSystem>();
            services.AddWithLifetime<IEmailWriter, EmailWriter>(serviceLifetime);
            services.AddWithLifetime<ISmtpSender, SmtpSender>(serviceLifetime);
            //services.Add<IProtocolLogger, NullProtocolLogger>(serviceLifetime);
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
