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
    public enum BackgroundTypeEnum
    {
        Solid,
        Mica,
        Acrylic,
    }

    public enum BackgroundSettingEnum
    {
        Color,
        Image,
    }

    public enum BackgroundImageType;
    public class ConfigOptions
    {
        public int ConfigVersion { get; } = 1;
        public string CanvasICSLink { get; set; } = "";
        public string School { get; set; } = "Arizona State University";
        public int BackgroundTransparency { get; set; } = 0;
        public string BackgroundColor { get; set; } = "#ff00ff";
        public string BackgroundImage { get; set; } = "";
        public BackgroundSettingEnum BackgroundSetting { get; set; } = BackgroundSettingEnum.Color;
        public BackgroundTypeEnum BackgroundType { get; set; } = BackgroundTypeEnum.Solid;
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
