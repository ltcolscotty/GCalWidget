using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Media;

namespace GCaLink.Models
{
    public class CalEventDisplay
    {
        public string Time { get; set; } = "";
        public string Title { get; set; } = "";
        public string Location { get; set; } = "";
        public Brush BackgroundBrush { get; set; } = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 58, 58, 58));
    }
}
