using System;
using Windows.UI;
using Microsoft.UI;
using Microsoft.UI.Xaml.Media;

namespace GCaLink.Services
{
    public static class ColorHelperService
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

            if (hex.Length < 6)
                throw new ArgumentException("Invalid hex color format.");

            byte r = Convert.ToByte(hex.Substring(start, 2), 16);
            byte g = Convert.ToByte(hex.Substring(start + 2, 2), 16);
            byte b = Convert.ToByte(hex.Substring(start + 4, 2), 16);

            return Color.FromArgb(a, r, g, b);
        }
    }
}