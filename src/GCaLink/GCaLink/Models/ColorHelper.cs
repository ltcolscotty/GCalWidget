using System;
using Google.Apis.Calendar.v3.Data;
using MessagePack;

namespace GCaLink.Models
{
    public static class ColorHelper
    {
        public static Color HexToColor(string hex)
        {
            if (string.IsNullOrWhiteSpace(hex))
                return Colors.Transparent;
            hex = hex.Replace("#", "");
            byte a = 255;
            int start = 0;
            if (hex.Length == 0)
            {
                a = Convert.ToByte(hex.Substring(0, 2), 16);
                start = 2;
            }
        }
    }
}