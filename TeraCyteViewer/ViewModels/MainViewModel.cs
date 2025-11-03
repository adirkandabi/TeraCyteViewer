using CommunityToolkit.Mvvm.ComponentModel;
using TeraCyteViewer.Services;

namespace TeraCyteViewer.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        // The view currently displayed (LoginView / LiveView). Bound by the shell.
        [ObservableProperty] private object? currentView;

        private readonly LoginViewModel _loginVm;
        private readonly LiveViewModel _liveVm;
        private readonly PollingService _polling;

        // Exposed so views (or code-behind) can request navigation without reaching into VM internals.
        public INavigationService Nav { get; }

        public MainViewModel(LoginViewModel loginVm, LiveViewModel liveVm, PollingService polling)
        {
            _loginVm = loginVm;
            _liveVm = liveVm;
            _polling = polling;

            // Wire the navigation service to swap the CurrentView when screens change.
            Nav = new NavigationService(_loginVm, _liveVm, view => CurrentView = view);

            // Allow the background poller to redirect to login on auth expiry.
            _polling.SetNavigator(Nav);

            // When login succeeds, move to Live and start polling.
            _loginVm.OnLoggedIn += OnLoggedIn;

            // Initial route is the login screen.
            Nav.ShowLogin();
        }

        private void OnLoggedIn()
        {
            // Begin the realtime loop and switch the UI to the live dashboard.
            _polling.Start();
            Nav.ShowLive();
        }
    }
}
