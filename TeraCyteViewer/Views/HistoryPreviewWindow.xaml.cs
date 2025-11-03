using System.Windows;
using System.Windows.Media.Animation;

namespace TeraCyteViewer.Views
{
    public partial class HistoryPreviewWindow : Window
    {
        private bool _isClosingAnimated;

        public HistoryPreviewWindow()
        {
            InitializeComponent();
            this.Closing += HistoryPreviewWindow_Closing;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            BeginFadeOutAndClose();
        }

        private void HistoryPreviewWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            
            if (!_isClosingAnimated)
            {
                e.Cancel = true;
                BeginFadeOutAndClose();
            }
        }

        private void BeginFadeOutAndClose()
        {
            if (_isClosingAnimated) return;
            _isClosingAnimated = true;

            if (Resources["FadeOutSb"] is Storyboard sb)
            {
                
                sb.Completed += (_, __) => this.Close();
                sb.Begin(this);
            }
            else
            {
               
                this.Close();
            }
        }
    }
}
