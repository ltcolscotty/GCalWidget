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
        private static Dictionary<string, EventTypeConfig> sourceConfigs;
        private static bool initializedAsyncStatus = false;
        private static List<string> activeSources = [];

        static SettingsRetriever() 
        {
            string appDataLocalPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string appDataLocalFolder = Path.Combine(appDataLocalPath, "GCWidget");
            string settingsFile = Path.Combine(appDataLocalFolder, "GCWConfig.json");
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
                // should exist, but vs still complains about possiblity of empty literal
                options = JsonSerializer.Deserialize<ConfigOptions>(jsonStr) ?? new ConfigOptions();
                options.Normalize();
            }
        }

        public static string GetBackgroundSetting() { return options.BackgroundSetting; }

        public static async void InitializeAsync(bool forceRefresh = false)
        {
            if (!initializedAsyncStatus && !forceRefresh) { return; }

            sourceConfigs = await LoadEventTypeConfigs(ETCSettingsFile);
            foreach (KeyValuePair<string, EventTypeConfig> source in sourceConfigs)
            {
                if (!source.Value.Enabled)
                {
                    sourceConfigs.Remove(source.Key);
                }
            }

            initializedAsyncStatus = true;
        }

        public static Dictionary<string, EventTypeConfig> GetSourceConfigs() { 
            InitializeAsync();
            return sourceConfigs; 
        }

        public static void SetCanvasICSLink(string newLink)
        {
            bool isValid = Uri.TryCreate(newLink, UriKind.Absolute, out Uri? uriResult)
                && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
            if (isValid)
            {
                options.CanvasICSLink = newLink;
            }
        }

        public static string GetCanvasICSLink() { return options.CanvasICSLink; }
        public static void SetCanvasEnabled(bool enabled) { options.CanvasEnabled = enabled; }
        public static void SetGoogleEnabled(bool enabled) { options.GoogleEnabled = enabled; }
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
