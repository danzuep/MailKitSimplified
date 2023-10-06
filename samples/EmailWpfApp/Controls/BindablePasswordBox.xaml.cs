using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace EmailWpfApp.Controls
{
    /// <summary>
    /// Interaction logic for BindablePasswordBox.xaml
    /// <see href="https://github.com/SingletonSean/wpf-mvvm-password-box/blob/master/PasswordBoxMVVM/Components/BindablePasswordBox.xaml.cs"/>
    /// </summary>
    public partial class BindablePasswordBox : UserControl
    {
        protected override void OnInitialized(EventArgs e)
        {
            InitializeComponent();
            base.OnInitialized(e);
        }

        private bool _isPasswordChanging;

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            _isPasswordChanging = true;
            Password = passwordBox.Password;
            _isPasswordChanging = false;
        }

        private void UpdatePassword()
        {
            if (!_isPasswordChanging)
            {
                passwordBox.Password = Password;
            }
        }

        private static void PasswordPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is BindablePasswordBox passwordBox)
            {
                passwordBox.UpdatePassword();
            }
        }

        private static readonly FrameworkPropertyMetadata _frameworkPropertyMetadata = new FrameworkPropertyMetadata(
                string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, PasswordPropertyChanged, null, false, UpdateSourceTrigger.PropertyChanged);

        public static readonly DependencyProperty PasswordProperty = DependencyProperty.Register(
            "Password", typeof(string), typeof(BindablePasswordBox), _frameworkPropertyMetadata);

        public string Password
        {
            get => (string)GetValue(PasswordProperty);
            set => SetValue(PasswordProperty, value);
        }
    }
}