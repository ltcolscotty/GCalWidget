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
        private static GoogleCalService GCS = new GoogleCalService(new GoogleCalOptions());
        private static Dictionary<string, bool>? sourceList;

        static  EventAggService()
        {

        }

        private static async Task AsyncInitialize()
        {
            sourceList = await SettingsRetriever.getActiveSources(GCS);
        }

        public static bool GetGoogleStatus()
        {
            if (sourceList == null)
            {
                LoggerService.LogWarning("EventAggService: Attempted to get google status when sourceList not initialized");
                return false;
            }
            return sourceList["google"];
        }

        public static async Task WriteUpcomingEventsMessagePackAsync(string outputPath = "data.msgpack")
        {
            sourceList = await SettingsRetriever.getActiveSources(GCS);
            List<CalEventDto> calendarData = [];
            if (sourceList["google"])
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
