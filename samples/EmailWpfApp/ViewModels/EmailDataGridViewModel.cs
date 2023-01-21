using EmailWpfApp.DataModel;
using EmailWpfApp.Models;
using EmailWpfApp.Helpers;
using System;
using System.Linq;
using System.Windows.Input;
using System.Collections.ObjectModel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace EmailWpfApp.ViewModels
{
    public class EmailDataGridViewModel : BaseViewModel
    {
        #region Public Properties
        public ObservableCollection<Email> ViewModelDataGrid { get; private set; } = new();

        public ICommand UserCommand { get; set; }
        #endregion

        private int _count = 0;

        public EmailDataGridViewModel()
        {
            UserCommand = new RelayCommand(ReceiveMail);
            StatusText = string.Empty;
            GetEmployees();
        }

        private void GetEmployees()
        {
            try
            {
                if (App.ServiceProvider?.GetService<EmailDbContext>() is EmailDbContext dbContext)
                {
                    var emails = dbContext.Emails.AsEnumerable();
                    var collection = new ObservableCollection<Email>(emails);
                    ViewModelDataGrid = collection;
                }
            }
            catch (Exception ex)
            {
                StatusText = ex.GetBaseException().Message;
                if (App.ServiceProvider?.GetService<ILogger<EmailDataGridViewModel>>() is ILogger logger)
                    logger.LogError(ex, StatusText);
                else
                    System.Diagnostics.Debugger.Break();
            }
        }

        private void ReceiveMail()
        {
            StatusText = $"Email #{++_count} received!";
            var email = Email.Write
                .From("admin@localhost", "Admin")
                .To($"person{_count}@example.com")
                .Subject($"Email #{_count}")
                .AsEmail;
            ViewModelDataGrid.Add(email);
            if (!string.IsNullOrWhiteSpace(StatusText))
            {
                logger.LogDebug("Result: {0}", StatusText);
            }
        }
    }
}
