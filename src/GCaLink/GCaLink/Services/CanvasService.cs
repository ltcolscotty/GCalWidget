using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ical.Net;

using GCaLink.Models;
using Ical.Net.CalendarComponents;

namespace GCaLink.Services
{
    internal class CanvasService
    {
        IcsDownloader downloader;
        public CanvasService() { 
            downloader = new IcsDownloader();
        }

        private CalEventDto Normallize(CalendarEvent inputEvent)
        {
            // Placeholder for regex and all the fun stuff
            CalEventDto eventObj = new CalEventDto();

            return eventObj;
        }

        public async Task FetchUpcomingEventsAsync(string sourceLink, List<CalEventDto> eventList)
        {
            // May need to check that folder exists
            string appDataLocalPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string appDataLocalFolder = Path.Combine(appDataLocalPath, "GCWidget");
            string calendarFile = Path.Combine(appDataLocalFolder, "CanvasData.ics");

            string expectedPath = await downloader.DownloadIcsAsync(sourceLink, calendarFile);
            if (expectedPath != calendarFile) {
                // Make a proper error logger later
                Console.WriteLine($"CanvasService: Unexpected handling of ics download: {expectedPath}");
                return;
            }

            string icsContent = File.ReadAllText(expectedPath);
            var calendar = Calendar.Load(icsContent);

            if (calendar == null) return;

            foreach (var calendarEvent in calendar.Events)
            {
                if (calendarEvent == null) continue;
                eventList.Add(Normallize(calendarEvent));
            }
        }
    }
}
