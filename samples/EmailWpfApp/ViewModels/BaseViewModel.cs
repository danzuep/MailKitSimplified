using System;
using System.Windows;
using Microsoft.Extensions.Logging;
using CommunityToolkit.Mvvm.ComponentModel;
using EmailWpfApp.Helpers;

namespace EmailWpfApp.ViewModels
{
    /// <summary>
    /// A base view model that can fire Property Changed events and contains a logger.
    /// </summary>
    public class BaseViewModel : ObservableObject
    {
        #region Logger and Status Text
        internal static ILogger logger = LogProvider.GetLogger<BaseViewModel>();
        public static readonly string StartupText = "Loading...";

        private string _statusText = StartupText;
        public string StatusText
        {
            get { return _statusText; }
            set
            {
                SetProperty(ref _statusText, value);

                if (!string.IsNullOrWhiteSpace(_statusText) &&
                    !_statusText.StartsWith(StartupText))
                {
                    logger.LogDebug("Status: {0}", _statusText);
                }
            }
        }
        public IProgress<string> ProgressStatus;
        #endregion

        #region Default Constructor
        public BaseViewModel()
        {
            ProgressStatus = new Progress<string>(UpdateStatusText);
        }
        #endregion

        #region Progress Update and Error Handling
        /// <summary>
        /// Delegate to update the StatusText in a thread-safe way,
        /// in a separate thread for the GUI.
        /// </summary>
        /// <param name="status">Text to display</param>
        internal void UpdateStatusText(string status)
        {
            StatusText = status;
        }

        internal void UpdateStatusText(Exception ex, string? status = null)
        {
            if (status == null)
            {
                var e = ex.GetBaseException();
                status = $"{e.GetType().Name}: {e.Message}";
            }
            StatusText = status;
        }

        internal void ShowAndLogWarning(Exception ex, string? status = null)
        {
            UpdateStatusText(ex, status);
            logger.LogWarning(ex, status);
            MessageBox.Show(ex.ToString(), status, MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        internal void ShowAndLogError(Exception ex, string? status = null)
        {
            UpdateStatusText(ex, status);
            logger.LogError(ex, status);
            MessageBox.Show(ex.ToString(), status, MessageBoxButton.OK, MessageBoxImage.Error);
        }
        #endregion
    }
}
