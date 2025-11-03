using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using TeraCyteViewer.Models;

namespace TeraCyteViewer.Views
{
    /// <summary>
    /// Interaction logic for LiveView.xaml
    /// </summary>
    public partial class LiveView : UserControl
    {
        public LiveView()
        {
            InitializeComponent();
        }
        private void HistoryItem_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.DataContext is ImageResultItem item)
            {
                var dlg = new Views.HistoryPreviewWindow
                {
                    Owner = Window.GetWindow(this),
                    DataContext = item
                };
                dlg.ShowDialog();
            }
        }
    }
}
