using System;
using System.Windows.Media.Imaging;

namespace TeraCyteViewer.Models
{
    public class ImageResultItem
    {
        public string ImageId { get; set; } = "";
        public BitmapImage? Image { get; set; }
        public string Label { get; set; } = "";
        public double Intensity { get; set; }
        public double Focus { get; set; }
        public DateTime Timestamp { get; set; }
        public int[]? Histogram { get; set; }
    }
}
