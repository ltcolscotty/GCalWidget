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
using System.Collections;

namespace GCaLink.Services
{
    internal class CanvasService
    {
        IcsDownloader downloader;
        UniRegexService uniRegexService;
        public CanvasService() { 
            downloader = new IcsDownloader();
            uniRegexService = new UniRegexService(SettingsRetriever.GetSchoolName());
        }

        private CalEventDto? Normalize(CalendarEvent inputEvent, IDHelper.EventID id)
        {
            if ((inputEvent.Summary == null) ||
                (inputEvent.Uid == null) ||
                (inputEvent.Start == null))
            {
                LoggerService.LogWarning(
                    $"CanvasService.Normalize(): Skipping invalid event (UID: '{inputEvent.Uid ?? "<missing>"}', Summary: '{inputEvent.Summary ?? "<missing>"}', Start: '{inputEvent.Start?.ToString() ?? "<missing>"}').",
                    LoggerStatusEnum.WARNING);
                return null;
            }

            string sectionInfo = uniRegexService.GetSectionInfo(inputEvent.Summary);
            string assignmentName = uniRegexService.GetAssignmentName(inputEvent.Summary);
            string className = uniRegexService.GetClassName(sectionInfo);
            string sectionName = uniRegexService.GetSectionName(sectionInfo);

            CalEventDto eventObj = new CalEventDto();
            eventObj.Id = id;
            eventObj.Source = className;
            eventObj.LongSource = className + "_" + sectionName;
            eventObj.Title = assignmentName;
            eventObj.Datetime = inputEvent.Start.AsUtc;
            eventObj.Link = inputEvent.Url?.ToString() ?? "";

            return eventObj;
        }

        public async Task<(Dictionary<IDHelper.EventID, CalEventDto>, List<IDHelper.EventID>)> FetchUpcomingEventsAsync(
            string sourceLink, 
            Dictionary<IDHelper.EventID, CalEventDto> events, 
            Dictionary<string, EventTypeConfig> sourceList)
        {
            // May need to check that folder exists
            string appDataLocalPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string appDataLocalFolder = Path.Combine(appDataLocalPath, "GCWidget");
            string calendarFile = Path.Combine(appDataLocalFolder, "CanvasData.ics");
            List<IDHelper.EventID> sourceKeys = new();

            string expectedPath = await downloader.DownloadIcsAsync(sourceLink, calendarFile);
            if (expectedPath != calendarFile) {
                string message = $"CanvasService: ICS download returned unexpected path '{expectedPath}' (expected '{calendarFile}').";
                LoggerService.LogWarning(message, LoggerStatusEnum.ERROR);
                throw new InvalidOperationException(message);
            }

            string icsContent = File.ReadAllText(expectedPath);
            var calendar = Calendar.Load(icsContent);

            if (calendar == null)
            {
                const string message = "CanvasService: ICS download could not be parsed into a calendar; existing Canvas events were not updated.";
                LoggerService.LogWarning(message, LoggerStatusEnum.ERROR);
                throw new InvalidOperationException(message);
            }

            int skippedCount = 0;
            foreach (CalendarEvent? calendarEvent in calendar.Events)
            {
                if (calendarEvent == null) continue;
                IDHelper.EventID id = IDHelper.GetEventID(calendarEvent.Uid ?? "", "canvas");
                CalEventDto? newCED = Normalize(calendarEvent, id);
                if (newCED == null)
                {
                    skippedCount++;
                    continue;
                }
                sourceKeys.Add(id);
                events[id] = newCED;
            }

            LoggerService.LogWarning(
                $"CanvasService: Calendar refresh parsed {sourceKeys.Count} events and skipped {skippedCount} invalid events.",
                skippedCount > 0 ? LoggerStatusEnum.WARNING : LoggerStatusEnum.INFO);

            return (events, sourceKeys);
        }
    }
}
