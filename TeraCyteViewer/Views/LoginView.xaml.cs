using System.Windows;
using System.Windows.Controls;
using TeraCyteViewer.ViewModels;

namespace TeraCyteViewer.Views
{
    public partial class LoginView : UserControl
    {
        public LoginView() => InitializeComponent();

        private async void Login_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is LoginViewModel vm)
            {
                vm.Password = Pwd.Password;
                await vm.LoginCommand.ExecuteAsync(null);
            }
        }
    }
}
