using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;

namespace TeraCyteViewer.ViewModels
{
    public partial class LoginViewModel : ObservableObject
    {
        // Two-way bound credentials
        [ObservableProperty] private string username = "";
        [ObservableProperty] private string password = "";

        // UX state
        [ObservableProperty] private string? errorMessage;
        [ObservableProperty] private bool isBusy;

        // Raised on successful login; the view or shell decides where to navigate next
        public event Action? OnLoggedIn;

        private readonly Services.AuthService _auth;

        public LoginViewModel(Services.AuthService auth, IConfiguration cfg)
        {
            _auth = auth;

            // Optional defaults
            Username = cfg["Auth:DefaultUsername"] ?? "";
            Password = cfg["Auth:DefaultPassword"] ?? "";
        }

        // Bound to the login button
        [RelayCommand]
        private async Task LoginAsync()
        {
            try
            {
                IsBusy = true;
                ErrorMessage = null;

                var ok = await _auth.LoginAsync(Username, Password);
                if (ok)
                {
                    OnLoggedIn?.Invoke();
                }
                else
                {
                    // Keep message generic; avoid leaking details on failures
                    ErrorMessage = "Invalid username or password";
                }
            }
            catch (Exception ex)
            {
                // Surface a concise message to the UI; full details go to logs at the service level
                ErrorMessage = ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
