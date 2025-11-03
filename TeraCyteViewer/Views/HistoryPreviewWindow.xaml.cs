using System.Windows;
using System.Windows.Media.Animation;

namespace TeraCyteViewer.Views
{
    public partial class HistoryPreviewWindow : Window
    {
        // Prevents re-entrant close while the fade-out animation is running
        private bool _isClosingAnimated;

        public HistoryPreviewWindow()
        {
            InitializeComponent();
            this.Closing += HistoryPreviewWindow_Closing;
        }

        // Close button handler – routes through the animated close path
        private void Close_Click(object sender, RoutedEventArgs e)
        {
            BeginFadeOutAndClose();
        }

        // Intercept the default close to play the fade-out first
        private void HistoryPreviewWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            // Only hijack the first close attempt; subsequent call will be the real close
            if (!_isClosingAnimated)
            {
                e.Cancel = true;
                BeginFadeOutAndClose();
            }
        }

        // Plays the FadeOut storyboard if available, then closes the window
        private void BeginFadeOutAndClose()
        {
            if (_isClosingAnimated) return;
            _isClosingAnimated = true;

            if (Resources["FadeOutSb"] is Storyboard sb)
            {
                // Once the animation completes, finish the close
                sb.Completed += (_, __) => this.Close();
                sb.Begin(this);
            }
            else
            {
                // Fallback: no storyboard defined – close immediately
                this.Close();
            }
        }
    }
}
