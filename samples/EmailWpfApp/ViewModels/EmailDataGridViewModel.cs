using EmailWpfApp.DataModel;
using EmailWpfApp.Helpers;
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
            if (App.ServiceProvider?.GetService<EmailDbContext>() is EmailDbContext dbContext)
            {
                var employees = dbContext.Emails.AsEnumerable();
                var collection = new ObservableCollection<Email>(employees);
                ViewModelDataGrid = collection;
            }
        }

        private void ReceiveMail()
        {
            StatusText = $"Email #{++_count} received!";
            var email = new Email
            {
                Id = _count,
                FirstName = $"FN#{_count}",
                LastName = $"LN#{_count}"
            };
            ViewModelDataGrid.Add(email);
            if (!string.IsNullOrWhiteSpace(StatusText))
            {
                logger.LogDebug("Result: {0}", StatusText);
            }
        }
    }
}
