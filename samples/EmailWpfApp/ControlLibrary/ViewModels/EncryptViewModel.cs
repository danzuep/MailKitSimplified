using System;
using System.Windows;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using ControlLibrary.Helpers;

namespace EmailWpfApp.ViewModels
{
    class EncryptViewModel : BaseViewModel
    {
        #region Public Properties
        //public ObservableCollection<Item> ViewModelDataGrid { get; private set; }
        public ICommand UserCommand { get; set; }

        private string _userInputText = String.Empty;
        public string UserInputTextBox
        {
            get { return _userInputText; }
            set
            {
                _userInputText = value;
                NotifyPropertyChanged();

                if (!String.IsNullOrWhiteSpace(_userInputText))
                {
                    logger.LogDebug("User Input: {0}", _userInputText);
                }
            }
        }

        private string _resultText = String.Empty;
        public string ResultTextBox
        {
            get { return _resultText; }
            set
            {
                _resultText = value;
                NotifyPropertyChanged();

                if (!String.IsNullOrWhiteSpace(_resultText))
                {
                    logger.LogDebug("Result: {0}", _resultText);
                }
            }
        }
        #endregion

        public EncryptViewModel()
        {
            UserCommand = new RelayCommand(Encrypt);
            StatusText = String.Empty;
#if DEBUG
            //UserInputTextBox = "";
#endif
        }

        private void Encrypt()
        {
            ResultTextBox = UserInputTextBox; //CryptographyHelper.Encrypt(UserInputTextBox);
            Clipboard.SetData(DataFormats.Text, ResultTextBox);
            StatusText = "Encrypted text copied to clipboard.";
        }
    }
}