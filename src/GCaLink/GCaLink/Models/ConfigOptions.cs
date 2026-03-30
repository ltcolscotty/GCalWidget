using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Serialization;

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

        [JsonExtensionData]
        public Dictionary<string, JsonElement>? ExtraData { get; set; }

        public void Normalize()
        {
            // Keep this for later if settings config gets updated in released version
        }
    }
}
