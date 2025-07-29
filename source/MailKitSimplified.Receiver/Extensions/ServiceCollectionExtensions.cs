using System;
using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;
using System.Security.Authentication;
using MailKit;
using MailKit.Net.Imap;
using MailKitSimplified.Receiver.Abstractions;
using MailKitSimplified.Receiver.Models;
using MailKitSimplified.Receiver.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MailKitSimplified.Receiver
{
    [ExcludeFromCodeCoverage]
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Add the MailKitSimplified.Receiver configuration and services.
        /// Adds IOptions<<see cref="EmailReceiverOptions"/>>,
        /// IOptions<<see cref="FolderMonitorOptions"/>>,
        /// <see cref="IImapReceiver"/>, <see cref="IMailFolderClient"/>,
        /// <see cref="IMailFolderReader"/>, and <see cref="IMailFolderMonitor"/>.
        /// </summary>
        /// <param name="services">Collection of service descriptors.</param>
        /// <param name="configuration">Application configuration properties.</param>
        /// <param name="sectionNameImap">IMAP configuration section name.</param>
        /// <param name="sectionNameMonitor">Folder monitor configuration section name.</param>
        /// <param name="sectionNameMailbox">Mailbox options configuration section name.</param>
        /// <param name="sectionNameFolder">MailFolder client configuration section name.</param>
        /// <returns><see cref="IServiceCollection"/>.</returns>
        public static IServiceCollection AddMailKitSimplifiedEmailReceiver(this IServiceCollection services, IConfiguration configuration, string sectionNameImap = EmailReceiverOptions.SectionName, string sectionNameMonitor = FolderMonitorOptions.SectionName, string sectionNameMailbox = MailboxOptions.SectionName, string sectionNameFolder = EmailReceiverOptions.SectionName)
        {
            services.AddMailKitSimplifiedEmailReceiverOptions(configuration, sectionNameImap, sectionNameMonitor, sectionNameMailbox, sectionNameFolder);
            services.AddMailKitSimplifiedEmailReceiver(ServiceLifetime.Transient);
            return services;
        }

        /// <inheritdoc cref="AddMailKitSimplifiedEmailReceiver"/>
        public static IServiceCollection AddScopedMailKitSimplifiedEmailReceiver(this IServiceCollection services, IConfiguration configuration, string sectionNameImap = EmailReceiverOptions.SectionName, string sectionNameMonitor = FolderMonitorOptions.SectionName, string sectionNameMailbox = MailboxOptions.SectionName, string sectionNameFolder = EmailReceiverOptions.SectionName)
        {
            services.AddMailKitSimplifiedEmailReceiverOptions(configuration, sectionNameImap, sectionNameMonitor, sectionNameMailbox, sectionNameFolder);
            services.AddMailKitSimplifiedEmailReceiver(ServiceLifetime.Scoped);
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
        public static IServiceCollection AddWithLifetime<TService, TImplementation>(this IServiceCollection services, ServiceLifetime serviceLifetime)
            where TService : class
            where TImplementation : class, TService
        {
            var serviceDescriptor = new ServiceDescriptor(typeof(TService), typeof(TImplementation), serviceLifetime);
            services.Add(serviceDescriptor);
            return services;
        }

        /// <summary>
        /// Adds services of the specified lifetimes, interfaces, and implementations.
        /// </summary>
        /// <inheritdoc cref="AddWithLifetime"/>
        private static IServiceCollection AddLifetimeServices(this IServiceCollection services, ServiceLifetime serviceLifetime)
        {
            services.AddWithLifetime<IImapReceiver, ImapReceiver>(serviceLifetime);
            services.AddWithLifetime<IMailFolderClient, MailFolderClient>(serviceLifetime);
            services.AddWithLifetime<IMailFolderReader, MailFolderReader>(serviceLifetime);
            services.AddWithLifetime<IMailFolderMonitor, MailFolderMonitor>(serviceLifetime);
            return services;
        }

        /// <summary>
        /// Add the MailKitSimplified.Receiver services.
        /// Adds <see cref="IImapReceiver"/>, <see cref="IMailFolderClient"/>,
        /// <see cref="IMailFolderReader"/>, and <see cref="IMailFolderMonitor"/>.
        /// </summary>
        /// <param name="services">Collection of service descriptors.</param>
        /// <returns><see cref="IServiceCollection"/>.</returns>
        private static IServiceCollection AddMailKitSimplifiedEmailReceiver(this IServiceCollection services, ServiceLifetime serviceLifetime)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));
            // Add library dependencies
            services.AddMemoryCache();
            services.AddSingleton<IFileSystem, FileSystem>();
            // Add custom services to the container
            services.AddSingleton<ILogFileWriter, LogFileWriterQueue>();
            services.AddSingleton<IProtocolLogger, MailKitProtocolLogger>();
            services.AddSingleton<IImapReceiverFactory, ImapReceiverFactory>();
            services.AddSingleton<IMailFolderMonitorFactory, MailFolderMonitorFactory>();
            services.AddLifetimeServices(serviceLifetime);
            return services;
        }

        /// <summary>
        /// Add the MailKitSimplified.Receiver configuration.
        /// </summary>
        /// <inheritdoc cref="AddMailKitSimplifiedEmailReceiver"/>
        private static IServiceCollection AddMailKitSimplifiedEmailReceiverOptions(this IServiceCollection services, IConfiguration configuration, string sectionNameImap = EmailReceiverOptions.SectionName, string sectionNameMonitor = FolderMonitorOptions.SectionName, string sectionNameMailbox = MailboxOptions.SectionName, string sectionNameFolder = EmailReceiverOptions.SectionName)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));
            var imapSection = configuration.GetRequiredSection(sectionNameImap);
            services.Configure<EmailReceiverOptions>(imapSection);
            services.AddOptions<FolderMonitorOptions>(configuration, sectionNameMonitor);
            services.AddOptions<MailboxOptions>(configuration, sectionNameMailbox);
            services.AddOptions<FolderClientOptions>(configuration, sectionNameFolder);
            var protocolLoggerSection = imapSection.GetSection(ProtocolLoggerOptions.SectionName);
            services.Configure<ProtocolLoggerOptions>(protocolLoggerSection);
            var fileWriteSection = protocolLoggerSection.GetSection(FileWriterOptions.SectionName);
            var protocolLog = imapSection["ProtocolLog"];
            if (string.IsNullOrEmpty(fileWriteSection["FilePath"]) && !string.IsNullOrEmpty(protocolLog))
                fileWriteSection["FilePath"] = protocolLog;
            services.Configure<FileWriterOptions>(fileWriteSection);
            return services;
        }

        /// <summary>
        /// Add the MailKitSimplified.Receiver configuration, IOptions<<see cref="EmailReceiverOptions"/>>.
        /// </summary>
        /// <param name="services">Collection of service descriptors.</param>
        /// <param name="emailReceiverOptions">Email receiver options.</param>
        /// <returns><see cref="IServiceCollection"/>.</returns>
        public static IServiceCollection AddEmailReceiverOptions(this IServiceCollection services, Action<EmailReceiverOptions> emailReceiverOptions)
        {
            if (emailReceiverOptions == null)
                throw new ArgumentNullException(nameof(emailReceiverOptions));
            services.Configure(emailReceiverOptions);
            return services;
        }

        /// <summary>
        /// Add the MailKitSimplified.Receiver folder monitor configuration, IOptions<<see cref="FolderMonitorOptions"/>>.
        /// </summary>
        /// <param name="services">Collection of service descriptors.</param>
        /// <param name="folderMonitorOptions">Email folder monitor options.</param>
        /// <returns><see cref="IServiceCollection"/>.</returns>
        public static IServiceCollection AddFolderMonitorOptions(this IServiceCollection services, Action<FolderMonitorOptions> folderMonitorOptions)
        {
            if (folderMonitorOptions == null)
                throw new ArgumentNullException(nameof(folderMonitorOptions));
            services.Configure(folderMonitorOptions);
            return services;
        }

        internal static IServiceCollection AddOptions<T>(this IServiceCollection services, IConfiguration configuration, string sectionName = null, string suffix = "Options") where T : class
        {
            var section = configuration.GetOptionsSection<T>(sectionName, suffix);
            return services.Configure<T>(section);
        }

        internal static T GetOptions<T>(this IConfiguration configuration, string sectionName = null, string suffix = "Options") where T : class
        {
            var section = configuration.GetOptionsSection<T>(sectionName, suffix);
            return section.Get<T>();
        }

        private static IConfigurationSection GetOptionsSection<T>(this IConfiguration configuration, string sectionName = null, string suffix = "Options") where T : class
        {
            var className = typeof(T).Name;
            if (sectionName == null)
            {
                sectionName = className.EndsWith(suffix) ?
                    className.Remove(className.LastIndexOf(suffix, StringComparison.OrdinalIgnoreCase)) :
                    className;
            }
            return configuration.GetSection(sectionName);
        }

        [Obsolete("This is for development testing only.")]
        public static IServiceCollection AddTlsImapClient(this IServiceCollection services)
        {
            services.AddTransient<IImapClient>((serviceProvider) => {
                var client = new ImapClient();
                client.CheckCertificateRevocation = false;
                client.SslProtocols = SslProtocols.Tls12;
#if NET5_0_OR_GREATER
                client.SslProtocols |= SslProtocols.Tls13;
#endif
                client.ServerCertificateValidationCallback = (s, c, h, e) => true;
                return client;
            });
            return services;
        }
    }
}
