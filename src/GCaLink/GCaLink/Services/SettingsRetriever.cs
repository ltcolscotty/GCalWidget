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
    internal class SettingsRetriever
    {
        private ConfigOptions options;
        public SettingsRetriever() 
        {
            string appDataLocalPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string appDataLocalFolder = Path.Combine(appDataLocalPath, "GCWidget");
            string settingsFile = Path.Combine(appDataLocalFolder, "GCWConfig.json");
            string ETCsettingsFile = Path.Combine(appDataLocalFolder, "ETCSettings.msgpack");
            

            if (!Directory.Exists(appDataLocalFolder))
            {
                Directory.CreateDirectory(appDataLocalFolder);
            }

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

        public void setCanvasICSLink(string newLink)
        {
            this.options.CanvasICSLink = newLink;
        }

        public async Task<List<EventTypeConfig>> LoadEventTypeConfigs(string inputPath)
        {
            byte[] bytes = await File.ReadAllBytesAsync(inputPath);
            return MessagePackSerializer.Deserialize<List<EventTypeConfig>>(bytes);
        }
        public async Task<Dictionary<string, bool>> getActiveSources(GoogleCalService googleCalService)
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
