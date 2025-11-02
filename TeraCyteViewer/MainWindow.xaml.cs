using System.ComponentModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using TeraCyteViewer.ViewModels;

namespace TeraCyteViewer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow(MainViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;
            vm.PropertyChanged += VmOnPropertyChanged;
            RootContent.Content = vm.CurrentView;

        }
             private void VmOnPropertyChanged(object? s, PropertyChangedEventArgs e)
        {
            if (DataContext is MainViewModel vm && e.PropertyName == nameof(MainViewModel.CurrentView))
                RootContent.Content = vm.CurrentView;
        }
    }
}