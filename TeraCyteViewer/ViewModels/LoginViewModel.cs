using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Configuration;
using System;

namespace TeraCyteViewer.ViewModels
{
    public partial class LoginViewModel : ObservableObject
    {
        [ObservableProperty] private string username = "";
        [ObservableProperty] private string password = "";
        [ObservableProperty] private string? errorMessage;
        [ObservableProperty] private bool isBusy;

        public event Action? OnLoggedIn;

        private readonly Services.AuthService _auth;

        public LoginViewModel(Services.AuthService auth, IConfiguration cfg)
        {
            _auth = auth;
            Username = cfg["Auth:DefaultUsername"] ?? "";
            Password = cfg["Auth:DefaultPassword"] ?? "";
        }

        [RelayCommand]
        private async Task LoginAsync()
        {
            try
            {
                IsBusy = true; ErrorMessage = null;
                var ok = await _auth.LoginAsync(Username, Password);
                if (ok) OnLoggedIn?.Invoke();
                else ErrorMessage = "Invalid username or password";
            }
            catch (Exception ex) { ErrorMessage = ex.Message; }
            finally { IsBusy = false; }
        }
    }
}
