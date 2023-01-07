using System;
using System.Windows;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using ControlLibrary.Helpers;

namespace EmailWpfApp.ViewModels
{
    class DecryptViewModel : BaseViewModel
    {
        #region Public Properties
        public ICommand UserCommand { get; set; }

        private string _userInputText = String.Empty;
        public string UserInputTextBox
        {
            get => _userInputText;
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
            get => _resultText;
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

        public DecryptViewModel()
        {
            UserCommand = new RelayCommand(Decrypt);
            StatusText = String.Empty;
#if DEBUG
            //UserInputTextBox = "";
#endif
        }

        private void Decrypt()
        {
            ResultTextBox = UserInputTextBox; //CryptographyHelper.Decrypt(UserInputTextBox);
            Clipboard.SetData(DataFormats.Text, ResultTextBox);
            StatusText = "Decrypted text copied to clipboard.";
        }
    }
}
