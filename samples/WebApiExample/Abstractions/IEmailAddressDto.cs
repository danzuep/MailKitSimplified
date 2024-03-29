﻿namespace WebApiExample.Abstractions
{
    /// <summary>
    /// <see href="https://datatracker.ietf.org/doc/html/rfc8621#section-4.1.2.3">RFC 8621</see>
    /// </summary>
    public interface IEmailAddressDto
    {
        /// <summary>
        /// The "name" or "display-name" of the "mailbox".
        /// <see href="https://datatracker.ietf.org/doc/html/rfc5322#section-3.4">RFC 5322 name-addr and display-name</see>
        /// </summary>
        string Name { get; set; }

        /// <summary>
        /// The "email" address or "addr-spec" of the "mailbox".
        /// <see href="https://datatracker.ietf.org/doc/html/rfc5322#section-3.4.1">RFC 5322 addr-spec</see>
        /// </summary>
        string Email { get; set; }

#if NET5_0_OR_GREATER
        /// <summary>
        /// Email contact name and address as a string.
        /// </summary>
        /// <returns></returns>
        string ToString() => $"{Name} <{Email}>";
#endif
    }
}
