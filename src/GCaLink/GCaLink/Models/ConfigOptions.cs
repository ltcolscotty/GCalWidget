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
        public string School { get; set; } = "Arizona State University";
        public int BackgroundTransparency { get; set; } = 0;
        public string BackgroundColor { get; set; } = "#ff00ff";
        public string BackgroundImage { get; set; } = "";
        /* color
         * image
         */
        public string BackgroundSetting { get; set; } = "color";
        /* Solid
         * Mica
         * Acrylic
         */
        public string BackgroundType { get; set; } = "Solid";
        public string FontFamily {get; set; } = "Segoe UI";
        public int FontSize { get; set; } = 12;
        public bool GoogleEnabled { get; set; } = false;
        public bool CanvasEnabled { get; set; } = false;
        public int TrackedDays { get; set; } = 3;
        public List<string> PinnedEventsList { get; set; } = [];

        [JsonExtensionData]
        public Dictionary<string, JsonElement>? ExtraData { get; set; }

        public void Normalize()
        {
            // Keep this for later if settings config gets updated in released version
        }
    }
}
