using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TeraCyteViewer.Models;

namespace TeraCyteViewer.Views
{
    public partial class LiveView : UserControl
    {
        public LiveView()
        {
            InitializeComponent();
        }

        // Opens a modal preview of the clicked history item.
        // Uses the item's DataContext so the preview binds without extra plumbing.
        private void HistoryItem_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.DataContext is ImageResultItem item)
            {
                var dlg = new HistoryPreviewWindow
                {
                    Owner = Window.GetWindow(this), // keep it centered/blocked over the current window
                    DataContext = item               // pass the selected item to the dialog
                };

                dlg.ShowDialog();
            }
        }
    }
}
