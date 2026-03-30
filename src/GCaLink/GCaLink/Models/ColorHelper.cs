using System;
using Windows.UI;
using Microsoft.UI;
using Microsoft.UI.Xaml.Media;

namespace GCaLink.Models
{
    public static class ColorHelper
    {
        public static Color HexToColor(string hex)
        {
            if (string.IsNullOrWhiteSpace(hex))
                return Colors.White;
            hex = hex.Replace("#", "");
            byte a = 255;
            int start = 0;
            if (hex.Length == 8)
            {
                a = Convert.ToByte(hex.Substring(0, 2), 16);
                start = 2;
            }
        }
    }
}