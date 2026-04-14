using GCaLink.Models;
using GCaLink.Services;

using Google.Apis.Calendar.v3;
using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;

namespace GCaLink.Services
{
    internal static class EventAggService
    {
        private static readonly GoogleCalService GCS = new GoogleCalService(new GoogleCalOptions());
        private static Dictionary<string, bool>? sourceList;
        private static Task? _initTask;

        public static GoogleCalService GetGoogleCalService() { return GCS; }

        public static Task InitializeAsync()
        {
            return _initTask ??= LoadSourcesAsync();
        }

        private static async Task LoadSourcesAsync()
        {
            sourceList = await SettingsRetriever.GetActiveSources(GCS);
        }

        public static async Task<bool> GetGoogleStatusAsync()
        {
            await InitializeAsync();

            if (sourceList == null || !sourceList.TryGetValue("google", out var enabled))
            {
                LoggerService.LogWarning("EventAggService: google status missing or sourceList not initialized");
                return false;
            }

            return enabled;
        }

        public static async Task WriteUpcomingEventsMessagePackAsync(string outputPath = "data.msgpack")
        {
            await InitializeAsync();

            var calendarData = new List<CalEventDto>();

            if (sourceList != null && sourceList.TryGetValue("google", out var enabled) && enabled)
            {
                CalendarService service = await GCS.CreateCalendarServiceAsync();
                await GCS.FetchUpcomingEventsAsync(service, calendarData);
            }

            byte[] bytes = MessagePackSerializer.Serialize(calendarData);
            await File.WriteAllBytesAsync(outputPath, bytes);
        }

        public static async Task<List<CalEventDto>> ReadUpcomingEventsMessagePackAsync(string inputPath = "data.msgpack")
        {
            byte[] bytes = await File.ReadAllBytesAsync(inputPath);
            return MessagePackSerializer.Deserialize<List<CalEventDto>>(bytes);
        }
    }
}
