using GCaLink.Models;
using GCaLink.Services;

using Google.Apis.Calendar.v3;
using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MessagePack.Formatters;
using System.ComponentModel;

namespace GCaLink.Services
{
    internal static class EventAggService
    {
        private static GoogleCalService GCS = new(SettingsRetriever.GetGoogleCalOptions());
        private static readonly CanvasService CanvasServ = new CanvasService();
        private static Dictionary<string, bool>? sourceList;
        private static Dictionary<string, List<IDHelper.EventID>> sourceIDs = new();
        private static readonly object eventsChangedLock = new();
        private static readonly CancellationTokenSource shutdownCancellation = new();
        private static readonly Timer eventsChangedDebounceTimer = new(
            _ => EventsChanged?.Invoke(null, EventArgs.Empty),
            null,
            Timeout.Infinite,
            Timeout.Infinite);
        private static FileSystemWatcher? eventDataWatcher;

        public static event EventHandler? EventsChanged;

        public static bool IsShutdownRequested => shutdownCancellation.IsCancellationRequested;

        public static void RequestShutdown()
        {
            if (!shutdownCancellation.IsCancellationRequested)
            {
                shutdownCancellation.Cancel();
            }
        }

        static EventAggService()
        {
            _ = LoadSourcesAsync();
            WatchCalendarDataFile();
        }

        private static void WatchCalendarDataFile()
        {
            string dataPath = SettingsRetriever.GetMainDataPath();
            string directory = Path.GetDirectoryName(dataPath)!;
            eventDataWatcher = new FileSystemWatcher(directory, Path.GetFileName(dataPath))
            {
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size
            };
            eventDataWatcher.Changed += (_, _) => QueueEventsChanged();
            eventDataWatcher.Created += (_, _) => QueueEventsChanged();
            eventDataWatcher.Renamed += (_, _) => QueueEventsChanged();
            eventDataWatcher.EnableRaisingEvents = true;
        }

        public static void NotifyViewsChanged()
        {
            lock (eventsChangedLock)
            {
                eventsChangedDebounceTimer.Change(150, Timeout.Infinite);
            }
        }

        private static void QueueEventsChanged() => NotifyViewsChanged();

        public static GoogleCalService GetGoogleCalService() { return GCS; }

        private static async Task<FileStream> AcquireCalendarDataLockAsync(CancellationToken cancellationToken)
        {
            string lockPath = SettingsRetriever.GetMainDataPath() + ".lock";

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    return new FileStream(lockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
                }
                catch (IOException exception) when ((exception.HResult & 0xFFFF) is 32 or 33)
                {
                    await Task.Delay(50, cancellationToken);
                }
            }
        }

        public static async Task ReloadGoogleServiceAsync()
        {
            GCS = new GoogleCalService(SettingsRetriever.GetGoogleCalOptions());
            await ReloadSourcesAsync();
        }

        public static async Task ReloadSourcesAsync()
        {
            await LoadSourcesAsync();
        }

        private static async Task LoadSourcesAsync()
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
                LoggerService.Log("EventAggService: google status missing or sourceList not initialized", LoggerStatusEnum.EXCEPTION);
                return false;
            }

            return enabled;
        }

        /// <summary>
        /// Reads calendar event message pack file and returns calendar events
        /// </summary>
        /// <param name="inputPath">File path for events MessagePack file</param>
        /// <returns>All eventIDs with affiliated CalEventDto objects</returns>
        public static async Task<Dictionary<IDHelper.EventID, CalEventDto>> ReadUpcomingEventsMessagePackAsync(string? inputPath)
        {
            if (inputPath == null)
            {
                inputPath = SettingsRetriever.GetMainDataPath();
            }

            CancellationToken cancellationToken = shutdownCancellation.Token;
            await using FileStream calendarDataLock = await AcquireCalendarDataLockAsync(cancellationToken);
            return await ReadCalendarDataWithoutLockAsync(inputPath, cancellationToken);
        }

        private static async Task<Dictionary<IDHelper.EventID, CalEventDto>> ReadCalendarDataWithoutLockAsync(
            string inputPath,
            CancellationToken cancellationToken)
        {
            byte[] bytes = await File.ReadAllBytesAsync(inputPath, cancellationToken);
            return MessagePackSerializer.Deserialize<Dictionary<IDHelper.EventID, CalEventDto>>(bytes);
        }

        private static async Task SaveCalDataAsync(
            Dictionary<IDHelper.EventID, CalEventDto> calendarData,
            string? outputPath,
            CancellationToken cancellationToken)
        {
            if (outputPath == null)
            {
                outputPath = SettingsRetriever.GetMainDataPath();
            }

            byte[] bytes = MessagePackSerializer.Serialize(calendarData);
            string temporaryPath = $"{outputPath}.{Guid.NewGuid():N}.tmp";
            try
            {
                await File.WriteAllBytesAsync(temporaryPath, bytes, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                File.Move(temporaryPath, outputPath, overwrite: true);
            }
            finally
            {
                if (File.Exists(temporaryPath))
                {
                    try
                    {
                        File.Delete(temporaryPath);
                    }
                    catch (Exception exception)
                    {
                        LoggerService.LogException(
                            "EventAggService: Failed to remove temporary calendar data file.",
                            exception);
                    }
                }
            }
        }

        /// <summary>
        /// Get events only from enabled sources
        /// </summary>
        /// <returns>Dictionary of keys of eventIDs with affiliated CalEventDto objects</returns>
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
            CancellationToken cancellationToken = shutdownCancellation.Token;
            await using FileStream calendarDataLock = await AcquireCalendarDataLockAsync(cancellationToken);
            bool? result = await RefreshCanvasWithoutLockAsync(cancellationToken);
            if (result == true)
            {
                SettingsRetriever.setLastUpdateTime(DateTimeOffset.Now);
            }
            return result;
        }

        private static async Task<bool?> RefreshCanvasWithoutLockAsync(CancellationToken cancellationToken)
        {
            if (sourceList == null)
            {
                LoggerService.Log("EventAggService: Attempted to get events on empty source list", LoggerStatusEnum.ERROR);
                return null;
            }

            if (!(sourceList.TryGetValue("canvas", out var cEnabled) && cEnabled))
            {
                LoggerService.Log("EventAggService: Attempted to refresh canvas events without enabled source", LoggerStatusEnum.WARNING);
                return false;
            }

            Dictionary<IDHelper.EventID, CalEventDto> calendarData = await ReadCalendarDataWithoutLockAsync(
                SettingsRetriever.GetMainDataPath(),
                cancellationToken);
            Dictionary<string, EventTypeConfig> sourceConfig = SettingsRetriever.GetSourceConfigs();

            if (sourceIDs.TryGetValue("canvas", out List<IDHelper.EventID>? canvasIds))
            {
                foreach (IDHelper.EventID id in canvasIds)
                {
                    calendarData.Remove(id);
                }
            }

            var (tCalendarData, keyList) = await CanvasServ.FetchUpcomingEventsAsync(
                SettingsRetriever.GetCanvasICSLink(),
                calendarData,
                sourceConfig,
                cancellationToken);
            calendarData = tCalendarData;

            await SaveCalDataAsync(calendarData, null, cancellationToken);
            sourceIDs["canvas"] = keyList;
            QueueEventsChanged();
            return true;
        }

        /// <summary>
        /// Granular control for refreshing only google sources
        /// </summary>
        /// <returns>Successful operation status</returns>
        public static async Task<bool?> RefreshGoogle()
        {
            CancellationToken cancellationToken = shutdownCancellation.Token;
            await using FileStream calendarDataLock = await AcquireCalendarDataLockAsync(cancellationToken);
            bool? result = await RefreshGoogleWithoutLockAsync(cancellationToken);
            if (result == true)
            {
                SettingsRetriever.setLastUpdateTime(DateTimeOffset.Now);
            }
            return result;
        }

        private static async Task<bool?> RefreshGoogleWithoutLockAsync(CancellationToken cancellationToken)
        {
            if (sourceList == null)
            {
                LoggerService.Log("EventAggService: Attempted to get events on empty source list", LoggerStatusEnum.ERROR);
                return null;
            }

            if (!(sourceList.TryGetValue("google", out var gEnabled) && gEnabled))
            {
                LoggerService.Log("EventAggService: Attempted to refresh google events without enabled source", LoggerStatusEnum.WARNING);
                return false;
            }

            CalendarService service = await GCS.CreateCalendarServiceAsync(cancellationToken);
            var (googleEvents, googleIds, isComplete) = await GCS.FetchUpcomingEventsAsync(
                service,
                new Dictionary<IDHelper.EventID, CalEventDto>(),
                cancellationToken);
            if (!isComplete)
            {
                return false;
            }

            Dictionary<IDHelper.EventID, CalEventDto> calendarData = await ReadCalendarDataWithoutLockAsync(
                SettingsRetriever.GetMainDataPath(),
                cancellationToken);
            foreach (IDHelper.EventID id in calendarData
                .Where(pair => IsGooglePrimaryEvent(pair.Value))
                .Select(pair => pair.Key)
                .ToList())
            {
                calendarData.Remove(id);
            }

            foreach ((IDHelper.EventID id, CalEventDto calendarEvent) in googleEvents)
            {
                calendarData[id] = calendarEvent;
            }

            await SaveCalDataAsync(calendarData, null, cancellationToken);
            sourceIDs["google"] = googleIds;
            QueueEventsChanged();
            return true;
        }

        private static bool IsGooglePrimaryEvent(CalEventDto calendarEvent)
        {
            if (calendarEvent.Provider == GoogleCalService.ProviderName &&
                calendarEvent.CalendarId == GoogleCalService.PrimaryCalendarId)
            {
                return true;
            }

            if (!string.IsNullOrWhiteSpace(calendarEvent.Provider) ||
                !Uri.TryCreate(calendarEvent.Link, UriKind.Absolute, out Uri? link))
            {
                return false;
            }

            bool isGoogleHost = link.Host.Equals("www.google.com", StringComparison.OrdinalIgnoreCase) ||
                link.Host.Equals("calendar.google.com", StringComparison.OrdinalIgnoreCase);
            return isGoogleHost && link.AbsolutePath.StartsWith("/calendar/event", StringComparison.OrdinalIgnoreCase);
        }

        public static async Task WriteUpcomingEventsMessagePackAsync(
            string? outputPath,
            CancellationToken cancellationToken = default)
        {
            using CancellationTokenSource linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(
                shutdownCancellation.Token,
                cancellationToken);
            cancellationToken = linkedCancellation.Token;
            await using FileStream calendarDataLock = await AcquireCalendarDataLockAsync(cancellationToken);

            if (sourceList == null)
            {
                LoggerService.Log("EventAggService: Attempted to get events on empty source list", LoggerStatusEnum.ERROR);
                return;
            }

            if (outputPath == null)
            {
                outputPath = SettingsRetriever.GetMainDataPath();
            }

            Dictionary<IDHelper.EventID, CalEventDto> calendarData = [];
            Dictionary<string, EventTypeConfig> sourceConfig = SettingsRetriever.GetSourceConfigs();
            Dictionary<string, List<IDHelper.EventID>> pendingSourceIds = new();

            if (sourceList.TryGetValue("google", out var gEnabled) && gEnabled)
            {
                CalendarService service = await GCS.CreateCalendarServiceAsync(cancellationToken);
                var (tCalendarData, keyList, isComplete) = await GCS.FetchUpcomingEventsAsync(
                    service,
                    calendarData,
                    cancellationToken);
                if (!isComplete)
                {
                    return;
                }
                calendarData = tCalendarData;
                pendingSourceIds["google"] = keyList;
            }

            if (sourceList.TryGetValue("canvas", out var cEnabled) && cEnabled)
            {
                var (tCalendarData, keyList) = await CanvasServ.FetchUpcomingEventsAsync(
                    SettingsRetriever.GetCanvasICSLink(),
                    calendarData,
                    sourceConfig,
                    cancellationToken);
                calendarData = tCalendarData;
                pendingSourceIds["canvas"] = keyList;
            }

            await SaveCalDataAsync(calendarData, outputPath, cancellationToken);
            foreach ((string source, List<IDHelper.EventID> ids) in pendingSourceIds)
            {
                sourceIDs[source] = ids;
            }
            SettingsRetriever.setLastUpdateTime(DateTimeOffset.Now);
            QueueEventsChanged();
        }

        /// <summary>
        /// Refresh all source events
        /// </summary>
        /// <returns></returns>
        public static async Task RefreshAllAsync()
        {
            CancellationToken cancellationToken = shutdownCancellation.Token;
            await using FileStream calendarDataLock = await AcquireCalendarDataLockAsync(cancellationToken);

            if (sourceList == null)
            {
                LoggerService.Log("EventAggService: Attempted to get events on empty source list", LoggerStatusEnum.ERROR);
                return;
            }

            // Wrapper prevents logging
            if (sourceList.TryGetValue("google", out var gEnabled) && gEnabled)
            {
                await RefreshGoogleWithoutLockAsync(cancellationToken);
            }

            if (sourceList.TryGetValue("canvas", out var cEnabled) && cEnabled)
            {
                await RefreshCanvasWithoutLockAsync(cancellationToken);
            }

            SettingsRetriever.setLastUpdateTime(DateTimeOffset.Now);
            LoggerService.Log("EventAggService: All sources refreshed successfully", LoggerStatusEnum.INFO);
        }
    }
}
