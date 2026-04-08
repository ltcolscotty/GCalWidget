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
        public static async Task WriteUpcomingEventsMessagePackAsync(string outputPath = "data.msgpack")
        {
            GoogleCalService GCS = new GoogleCalService(new GoogleCalOptions());
            Dictionary<string, bool> sourceList = await SettingsRetriever.getActiveSources(GCS);
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
