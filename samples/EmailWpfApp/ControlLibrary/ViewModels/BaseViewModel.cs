using System;
using System.Windows;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using ControlLibrary.Helpers;

namespace EmailWpfApp.ViewModels
{
    /// <summary>
    /// A base view model that can fire Property Changed events and contains a logger.
    /// </summary>
    public class BaseViewModel : INotifyPropertyChanged
    {
        #region Logger and Status Text
        internal static ILogger logger = LogProvider.GetLogger<BaseViewModel>();
        public const string StartupText = "Loading...";
        private string _statusText = StartupText;
        public string StatusText
        {
            get { return _statusText; }
            set
            {
                _statusText = value;
                NotifyPropertyChanged();

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

        internal void ShowAndLogWarning(Exception ex, string? message = null)
        {
            if (message == null)
                message = $"{ex.GetType().Name}: {ex.Message}";
            StatusText = message;
            logger.LogWarning(message);
            MessageBox.Show(ex.ToString(), message, MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        internal void ShowAndLogError(Exception ex, string? status = null)
        {
            if (status == null)
                status = $"{ex.GetType().Name}: {ex.Message}";
            StatusText = status;
            logger.LogError(ex, status);
            MessageBox.Show(ex.ToString(), status, MessageBoxButton.OK, MessageBoxImage.Error);
        }
        #endregion

        #region Property Changed Event Handling
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary> 
        /// This method can be called manually by the Set accessor of each property.
        /// The CallerMemberName attribute that is applied to the optional propertyName
        /// parameter causes the property name of the caller to be substituted as an argument.
        /// </summary>
        /// <param name="propertyName"></param>
        public void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}
