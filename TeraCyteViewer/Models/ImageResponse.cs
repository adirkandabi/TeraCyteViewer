using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeraCyteViewer.Models
{
    public sealed class ImageResponse
    {
        public string image_id { get; set; } = "";
        public string timestamp { get; set; } = "";
        public string image_data_base64 { get; set; } = "";
    }
}
