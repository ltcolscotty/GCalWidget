using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace GCaLink.Models
{
    public class ConfigOptions
    {
        public int ConfigVersion { get; } = 1;
        public string CanvasICSLink { get; set; } = "";
        public int BackgroundTransparency { get; set; } = 0;
        public string BackgroundColor { get; set; } = "#ff00ff";
        public string BackgroundImage { get; set; } = "";
        /* color
         * image
         */
        public string BackgroundSetting { get; set; } = "color";
        public Boolean UseAcrylic { get; set; } = true;
        public string FontFamily {get; set; } = "Segoe UI";
        public int FontSize { get; set; } = 12;
    }
}
