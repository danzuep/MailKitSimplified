using System;
using System.Windows;
using System.Windows.Input;
using System.Collections.ObjectModel;
using Microsoft.Extensions.Logging;
using EmailWpfApp.Helpers;

namespace EmailWpfApp.ViewModels
{
    class ReceiverViewModel : BaseViewModel
    {
        #region Public Properties
        public ObservableCollection<string> ViewModelData { get; private set; } = new ObservableCollection<string>();

        public ICommand UserCommand { get; set; }
        #endregion

        private int _count = 0;

        public ReceiverViewModel()
        {
            UserCommand = new RelayCommand(ReceiveMail);
            StatusText = string.Empty;
        }

        private void ReceiveMail()
        {
            ViewModelData.Add($"Email #{++_count}");
            StatusText = $"Email #{_count} received!";
            if (!string.IsNullOrWhiteSpace(StatusText))
            {
                logger.LogDebug("Result: {0}", StatusText);
            }
        }
    }
}
