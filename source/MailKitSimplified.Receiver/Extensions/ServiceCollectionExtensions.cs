using MailKit;
using MailKit.Net.Imap;
using System;
using System.IO.Abstractions;
using System.Diagnostics.CodeAnalysis;
using System.Security.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using MailKitSimplified.Receiver.Abstractions;
using MailKitSimplified.Receiver.Models;
using MailKitSimplified.Receiver.Services;

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
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));
            var imapSection = configuration.GetRequiredSection(sectionNameImap);
            services.Configure<EmailReceiverOptions>(imapSection);
            var monitorSection = configuration.GetSection(sectionNameMonitor);
            services.Configure<FolderMonitorOptions>(monitorSection);
            var mailboxSection = configuration.GetSection(sectionNameMailbox);
            services.Configure<MailboxOptions>(mailboxSection);
            var folderSection = configuration.GetRequiredSection(sectionNameFolder);
            services.Configure<FolderClientOptions>(folderSection);
            var protocolLoggerSection = imapSection.GetSection(ProtocolLoggerOptions.SectionName);
            services.Configure<ProtocolLoggerOptions>(protocolLoggerSection);
            var fileWriteSection = protocolLoggerSection.GetSection(FileWriterOptions.SectionName);
            var protocolLog = imapSection["ProtocolLog"];
            if (string.IsNullOrEmpty(fileWriteSection["FilePath"]) && !string.IsNullOrEmpty(protocolLog))
                fileWriteSection["FilePath"] = protocolLog;
            services.Configure<FileWriterOptions>(fileWriteSection);
            services.AddMailKitSimplifiedEmailReceiver();
            return services;
        }

        /// <summary>
        /// Add the MailKitSimplified.Receiver services.
        /// Adds <see cref="IImapReceiver"/>, <see cref="IMailFolderClient"/>,
        /// <see cref="IMailFolderReader"/>, and <see cref="IMailFolderMonitor"/>.
        /// </summary>
        /// <param name="services">Collection of service descriptors.</param>
        /// <returns><see cref="IServiceCollection"/>.</returns>
        private static IServiceCollection AddMailKitSimplifiedEmailReceiver(this IServiceCollection services)
        {
            // Add library dependencies
            services.AddMemoryCache();
            services.AddSingleton<IFileSystem, FileSystem>();
            // Add custom services to the container
            services.AddSingleton<ILogFileWriter, LogFileWriterQueue>();
            services.AddSingleton<IProtocolLogger, MailKitProtocolLogger>();
            services.AddSingleton<IImapReceiverFactory, ImapReceiverFactory>();
            services.AddSingleton<IMailFolderMonitorFactory, MailFolderMonitorFactory>();
            services.AddTransient<IImapReceiver, ImapReceiver>();
            services.AddTransient<IMailFolderClient, MailFolderClient>();
            services.AddTransient<IMailFolderReader, MailFolderReader>();
            services.AddTransient<IMailFolderMonitor, MailFolderMonitor>();
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
