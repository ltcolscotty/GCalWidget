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
    internal class EventAggService
    {
        public async Task WriteUpcomingEventsMessagePackAsync(string outputPath = "data.msgpack")
        {
            SettingsRetriever settingsRetriever = new SettingsRetriever();

            GoogleCalService GCS = new GoogleCalService(new GoogleCalOptions());

            Dictionary<string, bool> sourceList = await settingsRetriever.getActiveSources(GoogleCalService googleCalService)
            CalendarService service = await GCS.CreateCalendarServiceAsync();
            Dictionary<string, CalEventDto> calendarData = await GCS.FetchUpcomingEventsAsync(service);

            byte[] bytes = MessagePackSerializer.Serialize(calendarData);
            await File.WriteAllBytesAsync(outputPath, bytes);
        }

        public async Task<Dictionary<string, CalEventDto>> ReadUpcomingEventsMessagePackAsync(string inputPath = "data.msgpack")
        {
            byte[] bytes = await File.ReadAllBytesAsync(inputPath);
            return MessagePackSerializer.Deserialize<Dictionary<string, CalEventDto>>(bytes);
        }
    }
}
