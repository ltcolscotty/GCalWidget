using GCaLink.Models;
using Google.Apis.Auth;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
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
        private static List<string> activeSources = [];
        private static readonly string settingsFile;

        static SettingsRetriever() 
        {
            string appDataLocalPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string appDataLocalFolder = Path.Combine(appDataLocalPath, "GCWidget");
            settingsFile = Path.Combine(appDataLocalFolder, "GCWConfig.json");
            imageDataFolder = Path.Combine(appDataLocalFolder, "Images");
            dataFile = Path.Combine(appDataLocalFolder, "GCWMainData.msgpack");
            ETCSettingsFile = Path.Combine(appDataLocalFolder, "ETCSettings.msgpack");
            Directory.CreateDirectory(appDataLocalFolder);

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

        public static async void InitializeAsync(bool forceRefresh = false)
        {
            if (!initializedAsyncStatus && !forceRefresh) { return; }

            sourceConfigs = await LoadEventTypeConfigs(ETCSettingsFile);
            foreach (string sourceKey in sourceConfigs
                .Where(source => !source.Value.Enabled)
                .Select(source => source.Key)
                .ToList())
            {
                sourceConfigs.Remove(sourceKey);
            }

            initializedAsyncStatus = true;
        }

        public static Dictionary<string, EventTypeConfig> GetSourceConfigs() { 
            InitializeAsync();
            return sourceConfigs; 
        }

        public static bool SetCanvasICSLink(string newLink)
        {
            // NOTE: may need to change to only HTTPS? - Consider later
            if  (!(Uri.TryCreate(newLink, UriKind.Absolute, out Uri? uriResult)
               && (uriResult.Scheme == Uri.UriSchemeHttp 
                    || uriResult.Scheme == Uri.UriSchemeHttps))
                )
            {
                LoggerService.LogWarning($"Invalid url {newLink}", LoggerStatusEnum.WARNING);
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
                DefaultColor = options.BackgroundColor
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
        public static string GetImageDataFolder() { return imageDataFolder; }

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
