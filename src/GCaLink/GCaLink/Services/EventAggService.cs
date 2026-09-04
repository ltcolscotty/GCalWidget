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
using MessagePack.Formatters;
using System.ComponentModel;

namespace GCaLink.Services
{
    internal static class EventAggService
    {
        private static readonly GoogleCalService GCS = new GoogleCalService(new GoogleCalOptions());
        private static readonly CanvasService CanvasServ = new CanvasService();
        private static Dictionary<string, bool>? sourceList;
        private static Dictionary<string, List<IDHelper.EventID>> sourceIDs = new();

        static EventAggService()
        {
            LoadSourcesAsync();
        }

        public static GoogleCalService GetGoogleCalService() { return GCS; }

        private static async void LoadSourcesAsync()
        {
            sourceList = await SettingsRetriever.GetActiveSources(GCS);
            foreach (var(source, active) in sourceList)
            {
                if (!active)
                {
                    continue;
                }

                sourceIDs[source] = new();
            }
        }

        public static bool GetGoogleStatusAsync()
        {
            if (sourceList == null || !sourceList.TryGetValue("google", out var enabled))
            {
                LoggerService.LogWarning("EventAggService: google status missing or sourceList not initialized", LoggerStatusEnum.EXCEPTION);
                return false;
            }

            return enabled;
        }

        public static async Task<Dictionary<IDHelper.EventID, CalEventDto>> ReadUpcomingEventsMessagePackAsync(string? inputPath)
        {
            if (inputPath == null)
            {
                inputPath = SettingsRetriever.GetMainDataPath();
            }

            byte[] bytes = await File.ReadAllBytesAsync(inputPath);
            return MessagePackSerializer.Deserialize<Dictionary<IDHelper.EventID, CalEventDto>>(bytes);
        }

        private static async void SaveCalData(Dictionary<IDHelper.EventID, CalEventDto> calendarData, string? outputPath)
        {
            if (outputPath == null)
            {
                outputPath = SettingsRetriever.GetMainDataPath();
            }
            byte[] bytes = MessagePackSerializer.Serialize(calendarData);
            await File.WriteAllBytesAsync(outputPath, bytes);
        }

        public static async Task<Dictionary<IDHelper.EventID, CalEventDto>> GetFilteredDTO()
        {
            List<string> enabledSources = SettingsRetriever.GetActiveLongSources();
            DateTimeOffset targetDay = DateTimeOffset.Now.AddDays(SettingsRetriever.GetTrackedDays());
            Dictionary<IDHelper.EventID, CalEventDto> fullList = await ReadUpcomingEventsMessagePackAsync(null);
            foreach (var(id, calEvent) in fullList)
            {
                if (enabledSources.Contains(calEvent.LongSource) ||
                    calEvent.Datetime <= targetDay)
                { continue; }
                fullList.Remove(id);
            }
            return fullList;
        }

        public static async Task<bool?> RefreshCanvas()
        {
            if (sourceList == null)
            {
                LoggerService.LogWarning("EventAggService: Attempted to get events on empty source list", LoggerStatusEnum.ERROR);
                return null;
            }

            if (!(sourceList.TryGetValue("canvas", out var cEnabled) && cEnabled))
            {
                return false;
            }

            Dictionary<IDHelper.EventID, CalEventDto> calendarData = await ReadUpcomingEventsMessagePackAsync(null);
            Dictionary<string, EventTypeConfig> sourceConfig = SettingsRetriever.GetSourceConfigs();

            foreach (IDHelper.EventID id in sourceIDs["canvas"])
            {
                calendarData.Remove(id);
                sourceIDs["canvas"].Remove(id);
            }

            var (tCalendarData, keyList) = await CanvasServ.FetchUpcomingEventsAsync(SettingsRetriever.GetCanvasICSLink(), calendarData, sourceConfig);
            calendarData = tCalendarData;
            sourceIDs["canvas"] = keyList;

            SaveCalData(calendarData, null);
            return true;
        }

        public static async Task<bool?> RefreshGoogle()
        {
            if (sourceList == null)
            {
                LoggerService.LogWarning("EventAggService: Attempted to get events on empty source list", LoggerStatusEnum.ERROR);
                return null;
            }

            if (!(sourceList.TryGetValue("google", out var gEnabled) && gEnabled))
            {
                return false;
            }

            Dictionary<IDHelper.EventID, CalEventDto> calendarData = await ReadUpcomingEventsMessagePackAsync(null);
            CalendarService service = await GCS.CreateCalendarServiceAsync();

            foreach (IDHelper.EventID id in sourceIDs["google"])
            {
                calendarData.Remove(id);
                sourceIDs["google"].Remove(id);
            }
            var (tCalendarData, keyList) = await GCS.FetchUpcomingEventsAsync(service, calendarData);
            calendarData = tCalendarData;
            sourceIDs["google"] = keyList;

            SaveCalData(calendarData, null);
            return true;
        }

        public static async void WriteUpcomingEventsMessagePackAsync(string? outputPath)
        {
            if (sourceList == null)
            {
                LoggerService.LogWarning("EventAggService: Attempted to get events on empty source list", LoggerStatusEnum.ERROR);
                return;
            }

            if (outputPath == null)
            {
                outputPath = SettingsRetriever.GetMainDataPath();
            }

            Dictionary<IDHelper.EventID, CalEventDto> calendarData = [];
            Dictionary<string, EventTypeConfig> sourceConfig = SettingsRetriever.GetSourceConfigs();

            if (sourceList.TryGetValue("google", out var gEnabled) && gEnabled)
            {
                CalendarService service = await GCS.CreateCalendarServiceAsync();
                var(tCalendarData, keyList) = await GCS.FetchUpcomingEventsAsync(service, calendarData);
                calendarData = tCalendarData;
                sourceIDs["google"] = keyList;
            }

            if (sourceList.TryGetValue("canvas", out var cEnabled) && cEnabled)
            {
                var (tCalendarData, keyList) = await CanvasServ.FetchUpcomingEventsAsync(SettingsRetriever.GetCanvasICSLink(), calendarData, sourceConfig);
                calendarData = tCalendarData;
                sourceIDs["canvas"] = keyList;
            }

            SaveCalData(calendarData, null);
        }
    }
}
