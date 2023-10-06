using EmailWpfApp.ViewModels;

namespace EmailWpfApp.State.Navigators
{
    public class ViewModelDelegateRenavigator<TViewModel> : IRenavigator where TViewModel : BaseViewModel
    {
        private readonly INavigator _navigator;

        public ViewModelDelegateRenavigator(INavigator navigator)
        {
            _navigator = navigator;
        }
    }
}
