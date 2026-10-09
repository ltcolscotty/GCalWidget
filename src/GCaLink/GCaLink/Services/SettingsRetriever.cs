using GCaLink.Models;
using Google.Apis.Auth;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.ViewManagement;

using MessagePack;

namespace GCaLink.Services
{
    internal static class SettingsRetriever
    {
        private static ConfigOptions options;
        private static string ETCSettingsFile;
        private static string dataFile;
        private static string imageDataFolder;
        private static Dictionary<string, EventTypeConfig> sourceConfigs = new();
        private static bool initializedAsyncStatus = false;
        private static readonly SemaphoreSlim sourceConfigLoadLock = new(1, 1);
        private static FileSystemWatcher? sourceConfigWatcher;
        private static Timer? sourceConfigReloadTimer;
        private static List<string> activeSources = [];
        private static readonly string settingsFile;

        public static event Action<int>? WindowTransparencyChanged;

        static SettingsRetriever() 
        {
            string appDataLocalPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string appDataLocalFolder = Path.Combine(appDataLocalPath, "GCWidget");
            settingsFile = Path.Combine(appDataLocalFolder, "GCWConfig.json");
            imageDataFolder = Path.Combine(appDataLocalFolder, "SourceImages");
            dataFile = Path.Combine(appDataLocalFolder, "GCWMainData.msgpack");
            ETCSettingsFile = Path.Combine(appDataLocalFolder, "ETCSettings.msgpack");
            Directory.CreateDirectory(appDataLocalFolder);
            Directory.CreateDirectory(imageDataFolder);
            sourceConfigReloadTimer = new Timer(_ => _ = ReloadSourceConfigsFromDiskAsync(), null, Timeout.Infinite, Timeout.Infinite);
            sourceConfigWatcher = new FileSystemWatcher(appDataLocalFolder, Path.GetFileName(ETCSettingsFile))
            {
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size
            };
            sourceConfigWatcher.Changed += (_, _) => QueueSourceConfigReload();
            sourceConfigWatcher.Created += (_, _) => QueueSourceConfigReload();
            sourceConfigWatcher.Renamed += (_, _) => QueueSourceConfigReload();
            sourceConfigWatcher.EnableRaisingEvents = true;
            LoggerService.Log($"SettingsRetriever: Watching source customization file '{ETCSettingsFile}'.");

            if (!File.Exists(settingsFile))
            {
                options = new ConfigOptions();
                File.WriteAllText(settingsFile, JsonSerializer.Serialize(options));
            }
            else
            {
                string jsonStr = File.ReadAllText(settingsFile);
                options = JsonSerializer.Deserialize<ConfigOptions>(jsonStr) ?? new ConfigOptions();
                options.Normalize();
            }
        }

        public static BackgroundSettingEnum GetBackgroundSetting() { return options.BackgroundSetting; }

        public static BackgroundTypeEnum GetBackgroundType() { return options.BackgroundType; }

        public static int GetWindowTransparency() { return Math.Clamp(options.BackgroundTransparency, 0, 100); }

        public static void SetWindowTransparency(int transparencyPercentage)
        {
            options.BackgroundTransparency = Math.Clamp(transparencyPercentage, 0, 100);
            SaveOptions();
            WindowTransparencyChanged?.Invoke(options.BackgroundTransparency);
        }

        public static async Task InitializeAsync(bool forceRefresh = false, bool notifyViews = true)
        {
            if (!forceRefresh && initializedAsyncStatus) { return; }

            await sourceConfigLoadLock.WaitAsync();
            bool loaded = false;
            try
            {
                if (!forceRefresh && initializedAsyncStatus) { return; }

                sourceConfigs = await LoadEventTypeConfigs(ETCSettingsFile);
                foreach (string sourceKey in sourceConfigs
                    .Where(source => !source.Value.Enabled)
                    .Select(source => source.Key)
                    .ToList())
                {
                    sourceConfigs.Remove(sourceKey);
                }

                initializedAsyncStatus = true;
                loaded = true;
                LoggerService.Log(
                    $"SettingsRetriever: Loaded {sourceConfigs.Count} enabled source customizations from '{ETCSettingsFile}' (forceRefresh={forceRefresh}).");
            }
            finally
            {
                sourceConfigLoadLock.Release();
            }

            if (loaded && notifyViews)
            {
                EventAggService.NotifyViewsChanged();
            }
        }

        public static Dictionary<string, EventTypeConfig> GetSourceConfigs() { 
            _ = InitializeAsync();
            return sourceConfigs; 
        }

        private static void QueueSourceConfigReload()
        {
            LoggerService.Log("SettingsRetriever: Source customization file changed; debounce reload queued.");
            sourceConfigReloadTimer?.Change(150, Timeout.Infinite);
        }

        private static async Task ReloadSourceConfigsFromDiskAsync()
        {
            try
            {
                await InitializeAsync(forceRefresh: true);
            }
            catch (Exception exception)
            {
                LoggerService.LogException("SettingsRetriever: Failed to reload source customizations.", exception);
            }
        }

        public static bool SetCanvasICSLink(string newLink)
        {
            // NOTE: may need to change to only HTTPS? - Consider later
            if  (!(Uri.TryCreate(newLink, UriKind.Absolute, out Uri? uriResult)
               && (uriResult.Scheme == Uri.UriSchemeHttp 
                    || uriResult.Scheme == Uri.UriSchemeHttps))
                )
            {
                LoggerService.Log($"Invalid url {newLink}", LoggerStatusEnum.WARNING);
                return false;
            }
            options.CanvasICSLink = newLink;
            SaveOptions();
            return true;
        }

        public static string GetCanvasICSLink() { return options.CanvasICSLink; }
        public static bool GetCanvasEnabled() { return options.CanvasEnabled; }
        public static void SetCanvasEnabled(bool enabled)
        {
            options.CanvasEnabled = enabled;
            SaveOptions();
        }
        public static bool GetPinnedViewEnabled() { return options.PinnedViewEnabled; }
        public static void SetPinnedViewEnabled(bool enabled)
        {
            options.PinnedViewEnabled = enabled;
            SaveOptions();
        }
        public static bool GetPrimaryViewEnabled() { return options.PrimaryViewEnabled; }
        public static void SetPrimaryViewEnabled(bool enabled)
        {
            options.PrimaryViewEnabled = enabled;
            SaveOptions();
        }
        public static PrimaryViewEnum GetPrimaryView() { return options.PrimaryView; }
        public static void SetPrimaryView(PrimaryViewEnum primaryView)
        {
            options.PrimaryView = primaryView;
            SaveOptions();
        }
        public static WindowPosition? GetWindowPosition(bool pinned)
        {
            return pinned ? options.PinnedViewPosition : options.PrimaryViewPosition;
        }
        public static void SetWindowPosition(bool pinned, int x, int y)
        {
            WindowPosition position = new() { X = x, Y = y };
            if (pinned)
            {
                options.PinnedViewPosition = position;
            }
            else
            {
                options.PrimaryViewPosition = position;
            }

            SaveOptions();
        }
        public static WindowSize? GetWindowSize(bool pinned)
        {
            return pinned ? options.PinnedViewSize : options.PrimaryViewSize;
        }
        public static void SetWindowSize(bool pinned, int width, int height)
        {
            WindowSize size = new() { Width = width, Height = height };
            if (pinned)
            {
                options.PinnedViewSize = size;
            }
            else
            {
                options.PrimaryViewSize = size;
            }

            SaveOptions();
        }
        public static void SetGoogleEnabled(bool enabled)
        {
            options.GoogleEnabled = enabled;
            SaveOptions();
        }
        public static bool GetGoogleEnabled() { return options.GoogleEnabled; }

        public static void SetBackgroundType(BackgroundTypeEnum backgroundType)
        {
            options.BackgroundType = backgroundType;
            SaveOptions();
        }
        public static GoogleCalOptions GetGoogleCalOptions()
        {
            return new GoogleCalOptions
            {
                ClientId = options.GoogleClientId,
                ClientSecret = options.GoogleClientSecret,
                TokenPath = Path.Combine(Path.GetDirectoryName(settingsFile)!, "GoogleToken", "token.json"),
                DefaultColor = options.BackgroundColor,
                MaximumEventsPerRefresh = options.GoogleMaxEventsPerRefresh
            };
        }

        public static void SetGoogleCredentials(string clientId, string clientSecret)
        {
            options.GoogleClientId = clientId.Trim();
            options.GoogleClientSecret = clientSecret.Trim();
            SaveOptions();
        }

        public static void SaveOptions()
        {
            File.WriteAllText(settingsFile, JsonSerializer.Serialize(options, new JsonSerializerOptions
            {
                WriteIndented = true
            }));
        }

        public static string GetSchoolName() { return options.School; }
        public static string GetMainDataPath() { return dataFile; }
        public static bool GetInitializedStatus() { return initializedAsyncStatus; }
        public static int GetTrackedDays() { return options.TrackedDays; }
        public static int GetUpdateDurationMins() { return options.updateDurationMins; }
        public static string GetImageDataFolder() { return SourceImageService.Instance.ManagedImageDirectory; }

        private static async Task<Dictionary<string, EventTypeConfig>> LoadEventTypeConfigs(string inputPath)
        {
            byte[] bytes;
            if (!File.Exists(inputPath))
            {
                Dictionary<string, EventTypeConfig> configDict = [];
                bytes = MessagePackSerializer.Serialize(configDict);
                await File.WriteAllBytesAsync(inputPath, bytes);
            }
            else
            {
                bytes = await File.ReadAllBytesAsync(inputPath);
            }

            return MessagePackSerializer.Deserialize<Dictionary<string, EventTypeConfig>>(bytes);
        }

        public static List<string> GetActiveLongSources() { return activeSources; }

        public static async Task<Dictionary<string, bool>> GetActiveSources(GoogleCalService googleCalService)
        {
            bool googleActive = await googleCalService.IsAccountActiveAsync();
            return new Dictionary<string, bool>
            {
                { "canvas", !string.IsNullOrEmpty(options.CanvasICSLink) && options.CanvasEnabled },
                { "google", googleActive && options.GoogleEnabled }
            };
        }
    }
}
