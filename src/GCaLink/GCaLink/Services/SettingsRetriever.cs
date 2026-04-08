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
        private static bool initializedAsyncStatus = false;

        static SettingsRetriever() 
        {
            string appDataLocalPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string appDataLocalFolder = Path.Combine(appDataLocalPath, "GCWidget");
            string settingsFile = Path.Combine(appDataLocalFolder, "GCWConfig.json");
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

        public static async Task InitializeAsync()
        {
            List<EventTypeConfig> sourceConfigs = await LoadEventTypeConfigs(ETCSettingsFile);
            initializedAsyncStatus = true;
        }

        public static void setCanvasICSLink(string newLink)
        {
            bool isValid = Uri.TryCreate(newLink, UriKind.Absolute, out Uri? uriResult)
                && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
            if (isValid)
            {
                options.CanvasICSLink = newLink;
            }
        }

        public static string getSchoolName()
        {
            return options.School;
        }

        public static bool getInitializedStatus() { return initializedAsyncStatus; }

        private static async Task<List<EventTypeConfig>> LoadEventTypeConfigs(string inputPath)
        {
            byte[] bytes;
            if (!File.Exists(inputPath))
            {
                List<EventTypeConfig> configList = [];
                bytes = MessagePackSerializer.Serialize(configList);
                await File.WriteAllBytesAsync(inputPath, bytes);
            }
            else
            {
                bytes = await File.ReadAllBytesAsync(inputPath);
            }

            return MessagePackSerializer.Deserialize<List<EventTypeConfig>>(bytes);
        }

        public static async Task<Dictionary<string, bool>> getActiveSources(GoogleCalService googleCalService)
        {
            bool googleActive = await googleCalService.IsAccountActiveAsync();
            return new Dictionary<string, bool>
            {
                { "canvas", !string.IsNullOrEmpty(options.CanvasICSLink) },
                { "google", googleActive}
            };
        }
    }
}
