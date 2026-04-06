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
        public CanvasService() { 
            downloader = new IcsDownloader();
        }

        private CalEventDto Normalize(CalendarEvent inputEvent)
        {
            string class_data_regex = @"(?<=\[).*?(?=\])";
            string assignment_title_regex = @"^([^\[]*)";
            string class_name_regex = @"[A-Z]{3}[0-9]{3}";
            string section_regex = @"[0-9]{4}(.*?)[ABC]";
           
            string section_info  = Regex.Match(inputEvent.Summary, class_data_regex).Value.Trim();
            string assignment_name = Regex.Match(inputEvent.Summary, assignment_title_regex).Value.Trim();
            string class_name = Regex.Match(section_info, class_name_regex).Value.Trim();
            string section_name = Regex.Match(section_info, section_regex).Value.Trim();


            // Placeholder for regex and all the fun stuff
            CalEventDto eventObj = new CalEventDto();
            eventObj.Id = inputEvent.Uid;
            eventObj.Title = assignment_name;
            eventObj.Datetime = inputEvent.Start.DateTimeoffset;
            eventObj.Link = inputEvent.Url;

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
                eventList.Add(Normalize(calendarEvent));
            }
        }
    }
}
