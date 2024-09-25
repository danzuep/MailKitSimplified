using System.Windows;
using CommunityToolkit.Mvvm.DependencyInjection;
using EmailWpfApp.Data;
using EmailWpfApp.Extensions;
using EmailWpfApp.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace EmailWpfApp;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private readonly IHost _host;

    public App()
    {
        InitializeComponent();
        _host = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                services.AddSingleton<MainWindow>()
                    .AddSingleton<SenderViewModel>()
                    .AddSingleton<ReceiverViewModel>()
                    .AddSingleton<FolderMonitorViewModel>()
                    .ConfigureServices(context.Configuration);
                services.AddDbContext<EmailDbContext>(options =>
                    options.UseSqlite("Data Source=Email.db"));
            })
            .Build();
        Ioc.Default.ConfigureServices(_host.Services);
    }

    private void OnStartup(object sender, StartupEventArgs e)
    {
        var mainWindow = Ioc.Default.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        base.OnExit(e);
        _host.Dispose();
    }
}
