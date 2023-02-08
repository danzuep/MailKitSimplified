using System.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using EmailWpfApp.Extensions;
using CommunityToolkit.Mvvm.DependencyInjection;
using EmailWpfApp.ViewModels;

namespace EmailWpfApp
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();
            var services = new ServiceCollection()
                .AddSingleton<MainWindow>()
                .AddSingleton<SenderViewModel>()
                .AddSingleton<ReceiverViewModel>()
                .AddSingleton<FolderMonitorViewModel>()
                .ConfigureServices(configuration);
            var serviceProvider = services.BuildServiceProvider();
            Ioc.Default.ConfigureServices(serviceProvider);
        }

        private void OnStartup(object sender, StartupEventArgs e)
        {
            var mainWindow = Ioc.Default.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);
        }
    }
}
