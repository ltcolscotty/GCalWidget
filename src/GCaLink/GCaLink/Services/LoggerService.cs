using GCaLink.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GCaLink.Services
{
    internal static class LoggerService
    {
        private static readonly string logFilePath;
        private static readonly object lockObj = new();

        static LoggerService()
        {
            string appDataLocalPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string appDataLocalFolder = Path.Combine(appDataLocalPath, "GCWidget");
            Directory.CreateDirectory(appDataLocalFolder);

            logFilePath = Path.Combine(appDataLocalFolder, "GCWLogs.txt");

            if (!File.Exists(logFilePath))
            {
                using var _ = File.Create(logFilePath);
            }
        }

        public static void LogWarning(string warningText)
        {
            try
            {
                lock (lockObj)
                {
                    File.AppendAllText(logFilePath, $"{DateTimeOffset.UtcNow:O} [WARN] {warningText}{Environment.NewLine}");
                }
            }
            catch
            {
            }
        }
    }
}
