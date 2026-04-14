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
        private static readonly CanvasService CanvasServ = new CanvasService();
        private static Dictionary<string, bool>? sourceList;
        private static Task? _initTask;

        static EventAggService()
        {
            LoadSourcesAsync();
        }

        public static GoogleCalService GetGoogleCalService() { return GCS; }

        private static async void LoadSourcesAsync()
        {
            sourceList = await SettingsRetriever.GetActiveSources(GCS);
        }

        public static bool GetGoogleStatusAsync()
        {

            if (sourceList == null || !sourceList.TryGetValue("google", out var enabled))
            {
                LoggerService.LogWarning("EventAggService: google status missing or sourceList not initialized");
                return false;
            }

            return enabled;
        }

        public static async Task WriteUpcomingEventsMessagePackAsync(string outputPath = "data.msgpack")
        {

            List<CalEventDto> calendarData = [];

            if (sourceList == null) 
            {
                LoggerService.LogWarning("EventAggService: Attempted to get events on empty source list");
                return; 
            }

            if (sourceList.TryGetValue("google", out var gEnabled) && gEnabled)
            {
                CalendarService service = await GCS.CreateCalendarServiceAsync();
                await GCS.FetchUpcomingEventsAsync(service, calendarData);
            }

            if (sourceList.TryGetValue("canvas", out var cEnabled) && cEnabled)
            {
                await CanvasServ.FetchUpcomingEventsAsync(SettingsRetriever.GetCanvasICSLink(), calendarData);
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
