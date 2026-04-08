using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ical.Net;
using System.Text.RegularExpressions;

using GCaLink.Models;
using Ical.Net.CalendarComponents;

namespace GCaLink.Services
{
    internal class CanvasService
    {
        IcsDownloader downloader;
        UniRegexService uniRegexService;
        public CanvasService() { 
            downloader = new IcsDownloader();
            uniRegexService = new UniRegexService(SettingsRetriever.getSchoolName());

        }

        private CalEventDto? Normalize(CalendarEvent inputEvent)
        {
            // NOTE: LOG WHEN MISSING INFO

            if ((inputEvent.Summary == null) ||
                (inputEvent.Uid == null) ||
                (inputEvent.Start == null))
            {
                LoggerService.LogWarning("CanvasService.Normalize(): Nonexistent Summary, Start, or Uid for event, skipping over");
                return null;
            }

            string sectionInfo = uniRegexService.GetSectionInfo(inputEvent.Summary);
            string assignmentName = uniRegexService.GetAssignmentName(inputEvent.Summary);
            string className = uniRegexService.GetClassName(sectionInfo);
            string sectionName = uniRegexService.GetSectionName(sectionInfo);

            CalEventDto eventObj = new CalEventDto();
            eventObj.Id = inputEvent.Uid;
            eventObj.Source = className;
            eventObj.LongSource = className + "_" + sectionName;
            eventObj.Title = assignmentName;
            eventObj.Datetime = inputEvent.Start.AsUtc;
            eventObj.Link = inputEvent.Url?.ToString() ?? "";

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

            foreach (CalendarEvent? calendarEvent in calendar.Events)
            {
                if (calendarEvent == null) continue;
                CalEventDto? newCED = Normalize(calendarEvent);
                if (newCED == null) continue;
                eventList.Add(newCED);
            }
        }
    }
}
