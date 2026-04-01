using GCaLink.Models;
using Google.Apis.Auth;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

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
            bool isValid = Uri.TryCreate(newLink, UriKind.Absolute, out Uri? uriResult)
                && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
            if (isValid)
            {
                this.options.CanvasICSLink = newLink;
            }
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
