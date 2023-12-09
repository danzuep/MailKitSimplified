using System;
using System.Windows.Controls;

namespace EmailWpfApp.Pages
{
    /// <summary>
    /// Interaction logic for LoginPage.xaml
    /// </summary>
    public partial class LoginPage : Page
    {
        public LoginPage()
        {
            InitializeComponent();
            //var uri = new Uri("MainWindow.xaml", UriKind.Relative);
            this.NavigationService.Navigate(new LoginPage());
        }
    }
}
