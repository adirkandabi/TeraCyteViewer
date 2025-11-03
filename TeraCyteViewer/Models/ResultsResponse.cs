using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeraCyteViewer.Models
{
    public sealed class ResultsResponse
    {
        public string image_id { get; set; } = "";
        public double intensity_average { get; set; }
        public double focus_score { get; set; }
        public string classification_label { get; set; } = "";
        public System.Collections.Generic.List<int> histogram { get; set; } = new();
    }
}
