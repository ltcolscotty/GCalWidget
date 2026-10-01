using GCaLink.Models;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Globalization;
using System.IO;
using System.Linq;

namespace GCaLink.Services
{
    internal static class CalendarEventDisplayService
    {
        public static CalEventDisplay Create(CalEventDto calendarEvent, string time)
        {
            CalendarSourceIdentity identity = GetSourceIdentity(calendarEvent);
            string backgroundColor = SettingsRetriever.GetSourceConfigs()
                .FirstOrDefault(pair =>
                    pair.Key.Equals(identity.Key, StringComparison.OrdinalIgnoreCase) ||
                    pair.Value.Source.Equals(identity.SourceId, StringComparison.OrdinalIgnoreCase))
                .Value?.BkgColor ?? "#3A3A3A";
            string? imagePath = SourceImageService.Instance.GetSourceImagePath(identity);
            LoggerService.Log(
                $"CalendarEventDisplayService: Event '{calendarEvent.Title}' provider='{calendarEvent.Provider}' source='{calendarEvent.Source}' identity='{identity.Key}' imageMatched={!string.IsNullOrWhiteSpace(imagePath)} image='{imagePath ?? "<none>"}' color='{backgroundColor}'.");

            return new CalEventDisplay
            {
                Time = time,
                Title = calendarEvent.Title,
                BackgroundBrush = CreateBackgroundBrush(imagePath, backgroundColor)
            };
        }

        private static CalendarSourceIdentity GetSourceIdentity(CalEventDto calendarEvent)
        {
            if (string.Equals(calendarEvent.Provider, GoogleCalService.ProviderName, StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrWhiteSpace(calendarEvent.GoogleAccountEmail) &&
                !string.IsNullOrWhiteSpace(calendarEvent.CalendarId))
            {
                return CalendarSourceIdentity.CreateGoogle(
                    calendarEvent.GoogleAccountEmail,
                    calendarEvent.CalendarId);
            }

            string provider = calendarEvent.Provider;
            if (string.IsNullOrWhiteSpace(provider) && !string.IsNullOrWhiteSpace(calendarEvent.LongSource))
            {
                provider = "Canvas";
            }

            return CalendarSourceIdentity.FromValues(provider, calendarEvent.Source);
        }

        private static Brush CreateBackgroundBrush(string? imagePath, string backgroundColor)
        {
            if (!string.IsNullOrWhiteSpace(imagePath) && File.Exists(imagePath))
            {
                return new ImageBrush
                {
                    ImageSource = new BitmapImage(new Uri(imagePath, UriKind.Absolute)),
                    Stretch = Stretch.UniformToFill,
                    Opacity = 0.65
                };
            }

            if (TryParseColor(backgroundColor, out Windows.UI.Color color))
            {
                return new SolidColorBrush(color);
            }

            return new SolidColorBrush(Windows.UI.Color.FromArgb(255, 58, 58, 58));
        }

        private static bool TryParseColor(string colorText, out Windows.UI.Color color)
        {
            color = Windows.UI.Color.FromArgb(255, 58, 58, 58);
            string hex = colorText.Trim().TrimStart('#');
            if (hex.Length is not (6 or 8))
            {
                return false;
            }

            try
            {
                byte alpha = hex.Length == 8 ? byte.Parse(hex[..2], NumberStyles.HexNumber) : (byte)255;
                int offset = hex.Length == 8 ? 2 : 0;
                color = Windows.UI.Color.FromArgb(
                    alpha,
                    byte.Parse(hex.Substring(offset, 2), NumberStyles.HexNumber),
                    byte.Parse(hex.Substring(offset + 2, 2), NumberStyles.HexNumber),
                    byte.Parse(hex.Substring(offset + 4, 2), NumberStyles.HexNumber));
                return true;
            }
            catch (FormatException)
            {
                return false;
            }
        }
    }
}