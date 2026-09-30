using System;

namespace GCaLink.Models
{
    public readonly record struct CalendarSourceIdentity(string Provider, string SourceId)
    {
        public string NormalizedProvider => Provider?.Trim() ?? string.Empty;

        public string NormalizedSourceId => SourceId?.Trim() ?? string.Empty;

        public string Key => $"{NormalizedProvider}:{NormalizedSourceId}";

        public static CalendarSourceIdentity CreateGoogle(string accountEmail, string calendarId)
        {
            string sourceId = $"gcal|{Uri.EscapeDataString(accountEmail.Trim())}|{Uri.EscapeDataString(calendarId.Trim())}";
            return new CalendarSourceIdentity("Google", sourceId);
        }

        public static bool TryGetGoogleComponents(string sourceId, out string accountEmail, out string calendarId)
        {
            accountEmail = string.Empty;
            calendarId = string.Empty;
            string[] parts = (sourceId ?? string.Empty).Split('|');
            if (parts.Length != 3 || !parts[0].Equals("gcal", StringComparison.Ordinal))
            {
                return false;
            }

            try
            {
                accountEmail = Uri.UnescapeDataString(parts[1]);
                calendarId = Uri.UnescapeDataString(parts[2]);
                return !string.IsNullOrWhiteSpace(accountEmail) && !string.IsNullOrWhiteSpace(calendarId);
            }
            catch (UriFormatException)
            {
                return false;
            }
        }

        public static CalendarSourceIdentity FromValues(string provider, string sourceId)
        {
            return new CalendarSourceIdentity(
                provider ?? string.Empty,
                sourceId ?? string.Empty);
        }

        public bool IsEmpty => string.IsNullOrWhiteSpace(NormalizedProvider) || string.IsNullOrWhiteSpace(NormalizedSourceId);
    }
}
