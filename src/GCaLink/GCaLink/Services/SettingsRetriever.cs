using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using System.Text;
using System.Text.Json;

using GCaLink.Models;

namespace GCaLink.Services
{
    internal class SettingsRetriever
    {
        public SettingsRetriever() {
            string appDataLocalPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string appDataLocalFolder = Path.Combine(appDataLocalPath, "GCWidget");
            string settingsFile = Path.Combine(appDataLocalFolder, "GCWConfig.json");

            if (!Directory.Exists(appDataLocalFolder))
            {
                Directory.CreateDirectory(appDataLocalFolder);
            }

            if (!File.Exists(settingsFile))
            {
                ConfigOptions newOptions = new ConfigOptions();
                string jsonString = JsonSerializer.Serialize(newOptions);
            }
        }
    }
}
