﻿using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using MailKitSimplified.Receiver.Abstractions;
using MailKitSimplified.Receiver.Models;
using MailKitSimplified.Receiver.Services;
using MailKitSimplified.Sender.Abstractions;
using MailKitSimplified.Sender.Models;
using MailKitSimplified.Sender.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EmailWpfApp.ViewModels
{
    public sealed partial class LoginViewModel : BaseViewModel, IDisposable
    {
        private ISmtpSender _smtpSender;
        private IImapReceiver _imapReceiver;

        public LoginViewModel() : base()
        {
            _smtpSender = Ioc.Default.GetRequiredService<ISmtpSender>();
            _imapReceiver = Ioc.Default.GetRequiredService<IImapReceiver>();
            StatusText = string.Empty;
#if DEBUG
            var smtp = Ioc.Default.GetService<IOptions<EmailSenderOptions>>()?.Value;
            var imap = Ioc.Default.GetService<IOptions<EmailReceiverOptions>>()?.Value;
            SmtpHost = smtp?.SmtpHost ?? "localhost:25";
            ImapHost = imap?.ImapHost ?? "localhost:143";
            var cred = smtp?.SmtpCredential ?? imap?.ImapCredential;
            if (cred != null)
            {
                Username = cred.UserName;
                Password = cred.Password;
            }
#endif
        }

        [ObservableProperty]
        private string smtpHost;

        [ObservableProperty]
        private string imapHost;

        [ObservableProperty]
        private bool isInProgress;

        private CancellationTokenSource _cts = new();

        [RelayCommand]
        private async Task ConnectHostAsync()
        {
            if (IsInProgress)
            {
                _cts.Cancel();
                _cts = new();
                IsInProgress = false;
                return;
            }
            IsInProgress = true;
            try
            {
#if DEBUG
                var cred = new NetworkCredential(Username, Password);
                _smtpSender = SmtpSender.Create(SmtpHost, cred);
                _imapReceiver = ImapReceiver.Create(ImapHost, cred);
#endif
                StatusText = $"Connecting to SMTP {_smtpSender}...";
                await _smtpSender.ConnectSmtpClientAsync(_cts.Token);
                StatusText = $"Connected to SMTP {_smtpSender}. Connecting to IMAP {_imapReceiver}...";
                await _imapReceiver.ConnectAuthenticatedImapClientAsync(_cts.Token);
                StatusText = $"Connected to SMTP {_smtpSender} and IMAP {_imapReceiver}.";
            }
            catch (OperationCanceledException ex)
            {
                UpdateStatusText(ex);
            }
            catch (Exception ex)
            {
#if DEBUG
                logger.LogWarning(ex, ex.Message);
                var errorType = ex.GetBaseException().GetType().Name;
                UpdateStatusText($"{errorType}: login failed, check Docker is running if localhost was intentional.");
#else
                ShowAndLogError(ex);
#endif
                System.Diagnostics.Debugger.Break();
            }
            IsInProgress = false;
        }

        private string _username = string.Empty;
        public string Username
		{
			get => _username;
			set
			{
				_username = value;
				OnPropertyChanged(nameof(Username));
                OnPropertyChanged(nameof(CanLogin));
			}
		}

        private string _password = string.Empty;
        public string Password
        {
            get => _password;
            set
            {
                _password = value;
                OnPropertyChanged(nameof(Password));
                OnPropertyChanged(nameof(CanLogin));
			}
        }

		public bool CanLogin =>
            SmtpHost.StartsWith("localhost") || ImapHost.StartsWith("localhost") ||
            (!string.IsNullOrEmpty(Username) && !string.IsNullOrEmpty(Password));

        public void Dispose()
        {
            _smtpSender?.Dispose();
            _imapReceiver?.Dispose();
        }
    }
}
