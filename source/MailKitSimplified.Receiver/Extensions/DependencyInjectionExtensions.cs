using System;
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
        /// <summary>
        /// Add the MailKitSimplified.Receiver configuration and services.
        /// Adds IOptions<<see cref="EmailReceiverOptions"/>>,
        /// <see cref="IMailReader"/>, <see cref="IMailFolderClient"/>,
        /// <see cref="IMailFolderReader"/>, and <see cref="IImapReceiver"/>.
        /// </summary>
        /// <param name="services">Collection of service descriptors.</param>
        /// <param name="emailReceiverOptions">Email sender options.</param>
        /// <param name="configuration"></param>
        /// <param name="sectionName"></param>
        /// <returns></returns>
        public static IServiceCollection AddMailKitSimplifiedEmailReceiver(this IServiceCollection services, IConfiguration configuration, string sectionName = EmailReceiverOptions.SectionName)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));
            var configSection = configuration.GetRequiredSection(sectionName);
            services.Configure<EmailReceiverOptions>(configSection);
            services.AddMailKitSimplifiedEmailReceiver();
            return services;
        }

        /// <summary>
        /// Add the MailKitSimplified.Receiver configuration and services.
        /// Adds IOptions<<see cref="EmailReceiverOptions"/>>,
        /// <see cref="IMailReader"/>, <see cref="IMailFolderClient"/>,
        /// <see cref="IMailFolderReader"/>, and <see cref="IImapReceiver"/>.
        /// </summary>
        /// <param name="services">Collection of service descriptors.</param>
        /// <param name="emailReceiverOptions">Email sender options.</param>
        public static IServiceCollection AddMailKitSimplifiedEmailReceiver(this IServiceCollection services, Action<EmailReceiverOptions> emailReceiverOptions)
        {
            if (emailReceiverOptions == null)
                throw new ArgumentNullException(nameof(emailReceiverOptions));
            services.Configure(emailReceiverOptions);
            services.AddMailKitSimplifiedEmailReceiver();
            return services;
        }

        /// <summary>
        /// Add the MailKitSimplified.Receiver services.
        /// Adds <see cref="IMailReader"/>, <see cref="IMailFolderClient"/>,
        /// <see cref="IMailFolderReader"/>, and <see cref="IImapReceiver"/>.
        /// </summary>
        /// <param name="services">Collection of service descriptors.</param>
        private static IServiceCollection AddMailKitSimplifiedEmailReceiver(this IServiceCollection services)
        {
            services.AddTransient<IMailReader, MailReader>();
            services.AddTransient<IMailFolderClient, MailFolderClient>();
            services.AddTransient<IMailFolderReader, MailFolderReader>();
            services.AddTransient<IImapReceiver, ImapReceiver>();
            return services;
        }
    }
}
