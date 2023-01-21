using System.Windows;

namespace EmailWpfApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            var program = System.Reflection.Assembly.GetExecutingAssembly().GetName();
            Title = $"{program.Name} {program.Version}";
#if DEBUG
            Title += " Beta";
#endif
        }
    }
}
