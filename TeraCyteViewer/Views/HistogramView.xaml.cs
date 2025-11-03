using ScottPlot;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace TeraCyteViewer.Views
{
    public partial class HistogramView : UserControl
    {
        public HistogramView()
        {
            InitializeComponent();
            InitPlotStyle();
        }

        // Bound property for histogram data from the ViewModel
        public int[]? Data
        {
            get => (int[]?)GetValue(DataProperty);
            set => SetValue(DataProperty, value);
        }

        // DependencyProperty so WPF can bind the histogram array dynamically
        public static readonly DependencyProperty DataProperty =
            DependencyProperty.Register(
                nameof(Data),
                typeof(int[]),
                typeof(HistogramView),
                new PropertyMetadata(null, OnDataChanged));

        private static void OnDataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is HistogramView view)
                view.RenderPlot();
        }

        // Initializes consistent styling and axis labels for the histogram
        private void InitPlotStyle()
        {
            var plt = Plot.Plot;

            plt.Axes.Frameless(false);
            plt.Title("Intensity Histogram");
            plt.XLabel("Intensity (0–255)");
            plt.YLabel("Count");

            Plot.Refresh();
        }

        // Renders a new bar plot when the Data property updates
        private void RenderPlot()
        {
            var plt = Plot.Plot;
            plt.Clear();

            if (Data is not { Length: > 0 })
            {
                Plot.Refresh();
                return;
            }

            // Build X/Y values from the intensity array
            double[] xs = Enumerable.Range(0, Data.Length).Select(i => (double)i).ToArray();
            double[] ys = Data.Select(v => (double)v).ToArray();

            var bars = plt.Add.Bars(xs, ys);
            bars.Color = ScottPlot.Colors.Blue;

            plt.Axes.AutoScale();
            Plot.Refresh();
        }
    }
}
