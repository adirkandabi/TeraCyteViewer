using CommunityToolkit.Mvvm.ComponentModel;
using TeraCyteViewer.Services;

namespace TeraCyteViewer.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        [ObservableProperty] private object? currentView;

        private readonly LoginViewModel _loginVm;
        private readonly LiveViewModel _liveVm;
        private readonly PollingService _polling;
        public INavigationService Nav { get; }

        public MainViewModel(LoginViewModel loginVm, LiveViewModel liveVm, PollingService polling)
        {
            _loginVm = loginVm;
            _liveVm = liveVm;
            _polling = polling;

            Nav = new NavigationService(_loginVm, _liveVm, view => CurrentView = view);

            _polling.SetNavigator(Nav);

            _loginVm.OnLoggedIn += OnLoggedIn;

            Nav.ShowLogin();
        }

        private void OnLoggedIn()
        {
            _polling.Start();
            Nav.ShowLive();
        }
    }
}
