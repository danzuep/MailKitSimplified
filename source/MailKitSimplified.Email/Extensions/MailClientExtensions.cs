using MailKit;
using MailKit.Net.Imap;
using MailKit.Security;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MailKitSimplified.Email.Models;

namespace MailKitSimplified.Email.Extensions
{
    public static class MailClientExtensions
    {
        public static async ValueTask ConnectAsync(this IMailService client, string host, ushort port, CancellationToken cancellationToken = default)
        {
            await client.ConnectAsync(host, port, SecureSocketOptions.Auto, cancellationToken).ConfigureAwait(false);
            if (client is IImapClient imapClient && imapClient.Capabilities.HasFlag(ImapCapabilities.Compress))
                await imapClient.CompressAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Authenticating via a SASL mechanism may be a multi-step process.
        /// <see href="http://www.mimekit.net/docs/html/T_MailKit_Security_SaslMechanism.htm"/>
        /// <seealso href="http://www.mimekit.net/docs/html/T_MailKit_Security_SaslMechanismOAuth2.htm"/>
        /// </summary>
        public static async ValueTask AuthenticateAsync(this IMailService client, NetworkCredential credential, Func<IMailService, Task> authenticationMethod = null, CancellationToken cancellationToken = default)
        {
            if (authenticationMethod != null) // for XOAUTH2 and OAUTHBEARER
                await authenticationMethod(client).ConfigureAwait(false);
            else
            {
                var ntlm = client.AuthenticationMechanisms.Contains("NTLM") ?
                    new SaslMechanismNtlm(credential) : null;
                if (ntlm?.Workstation != null)
                    await client.AuthenticateAsync(ntlm, cancellationToken).ConfigureAwait(false);
                else
                    await client.AuthenticateAsync(credential, cancellationToken).ConfigureAwait(false);
            }
        }

        public static async ValueTask CheckConnectionAsync(this IMailService client, string host, ushort port, CancellationToken cancellationToken = default)
        {
            if (!client.IsConnected)
            {
                await client.ConnectAsync(host, port, cancellationToken).ConfigureAwait(false);
            }
        }

        public static async ValueTask CheckAuthenticationAsync(this IMailService client, NetworkCredential credential, Func<IMailService, Task> authenticationMethod = null, CancellationToken cancellationToken = default)
        {
            if (!client.IsAuthenticated)
            {
                await client.AuthenticateAsync(credential, authenticationMethod, cancellationToken).ConfigureAwait(false);
            }
        }

        public static async ValueTask<IMailService> CheckAsync(this IMailService client, EmailOptions emailOptions, CancellationToken cancellationToken = default)
        {
            await client.CheckConnectionAsync(emailOptions.Host, emailOptions.Port, cancellationToken).ConfigureAwait(false);
            await client.CheckAuthenticationAsync(emailOptions.Credential, emailOptions.AuthenticationMethod, cancellationToken).ConfigureAwait(false);
            return client;
        }
    }
}
