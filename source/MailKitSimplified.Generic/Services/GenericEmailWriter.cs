﻿using System;
using System.Threading;
using System.Threading.Tasks;
using MailKitSimplified.Generic.Abstractions;
using MailKitSimplified.Generic.Models;

namespace MailKitSimplified.Generic.Services
{
    public class GenericEmailWriter : IGenericEmailWriter
    {
        public IGenericEmail AsEmail => _email;

        private IGenericEmail _email = new GenericEmail();

        private IGenericEmailContact _defaultFrom = null;


        private readonly IGenericEmailSender _emailSender;

        public GenericEmailWriter(IGenericEmailSender emailSender)
        {
            _emailSender = emailSender ?? throw new ArgumentNullException(nameof(emailSender));
        }

        public IGenericEmailWriter Header(string key, string value)
        {
            _email.Headers.Add(key, value);
            return this;
        }

        public IGenericEmailWriter DefaultFrom(string emailAddress, string name = null)
        {
            _defaultFrom = GenericEmailContact.Create(emailAddress, name);
            _email.From.Add(_defaultFrom);
            return this;
        }

        public IGenericEmailWriter From(string emailAddress, string name = null)
        {
            _email.From.Add(GenericEmailContact.Create(emailAddress, name));
            return this;
        }

        public IGenericEmailWriter To(string emailAddress, string name = null)
        {
            _email.To.Add(GenericEmailContact.Create(emailAddress, name));
            return this;
        }

        public IGenericEmailWriter Cc(string emailAddress, string name = null)
        {
            _email.Cc.Add(GenericEmailContact.Create(emailAddress, name));
            return this;
        }

        public IGenericEmailWriter Bcc(string emailAddress, string name = null)
        {
            _email.Bcc.Add(GenericEmailContact.Create(emailAddress, name));
            return this;
        }

        public IGenericEmailWriter Subject(string subject)
        {
            _email.Subject = subject ?? string.Empty;
            return this;
        }

        public IGenericEmailWriter Subject(string prefix, string suffix)
        {
            _email.Subject = $"{prefix}{_email.Subject}{suffix}";
            return this;
        }

        public IGenericEmailWriter Attach(string key, object value = null)
        {
            _email.Attachments.Add(key, value);
            return this;
        }

        public IGenericEmailWriter BodyText(string plainText)
        {
            _email.BodyText = plainText ?? string.Empty;
            return this;
        }

        public IGenericEmailWriter BodyHtml(string htmlText)
        {
            _email.BodyHtml = htmlText ?? string.Empty;
            return this;
        }

        public IGenericEmailWriter Copy()
        {
            var copy = MemberwiseClone() as IGenericEmailWriter;
            return copy;
        }

        public void Send(CancellationToken cancellationToken = default) =>
            SendAsync(cancellationToken).ConfigureAwait(false).GetAwaiter().GetResult();

        public bool TrySend(CancellationToken cancellationToken = default) =>
            TrySendAsync(cancellationToken).ConfigureAwait(false).GetAwaiter().GetResult();

        public async Task SendAsync(CancellationToken cancellationToken = default)
        {
            await _emailSender.SendAsync(_email, cancellationToken).ConfigureAwait(false);
            _email = new GenericEmail();
            if (_defaultFrom != null)
                _email.From.Add(_defaultFrom);
        }

        public async Task<bool> TrySendAsync(CancellationToken cancellationToken = default)
        {
            bool isSent = await _emailSender.TrySendAsync(_email, cancellationToken).ConfigureAwait(false);
            _email = new GenericEmail();
            if (_defaultFrom != null)
                _email.From.Add(_defaultFrom);
            return isSent;
        }
    }
}
