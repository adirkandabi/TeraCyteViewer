using TeraCyteViewer.ViewModels;
using TeraCyteViewer.Views;
using System;

namespace TeraCyteViewer.Services
{
    public class NavigationService : INavigationService
    {
        private readonly LoginViewModel _loginVm;
        private readonly LiveViewModel _liveVm;
        private readonly Action<object> _setView;

        public NavigationService(LoginViewModel loginVm, LiveViewModel liveVm, Action<object> setView)
        {
            _loginVm = loginVm;
            _liveVm = liveVm;
            _setView = setView;
        }

        public void ShowLogin() => _setView(new LoginView { DataContext = _loginVm });
        public void ShowLive() => _setView(new LiveView { DataContext = _liveVm });
    }
}
