using EmailWpfApp.ViewModels;
using System;

namespace EmailWpfApp.State.Navigators
{
    public enum ViewType
    {
        Login,
        Send,
        Receive,
        Monitor
    }

    public interface INavigator
    {
        BaseViewModel? CurrentViewModel { get; set; }
        event Action? StateChanged;
    }
}
