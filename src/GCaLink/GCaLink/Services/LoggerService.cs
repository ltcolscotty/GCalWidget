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
        private static readonly long MaxFileSizeBytes = 1 * 1024 * 1024;
        private static readonly int MaxOldFiles = 3;

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

        public static void LogWarning(string warningText, bool isException=false)
        {
            try
            {
                lock (lockObj)
                {
                    FileInfo? info = new FileInfo(logFilePath);
                    if (info.Exists && info.Length >= MaxFileSizeBytes)
                    {
                        Rotate();
                    }

                    if (isException)
                    {
                        File.AppendAllText(logFilePath, $"{DateTimeOffset.UtcNow:O} [EXCEPTION] {warningText}{Environment.NewLine}");
                    } 
                    else
                    {
                        File.AppendAllText(logFilePath, $"{DateTimeOffset.UtcNow:O} [WARN] {warningText}{Environment.NewLine}");
                    }
                }
            }
            catch
            {
            }
        }

        private static void Rotate()
        {
            if (!File.Exists(logFilePath)) return;
            string current = logFilePath;
            string next;

            for (int i = MaxOldFiles - 1; i >= 0; i--)
            {
                string src = i == 0 ? current : $"{current}.{i}";
                next = $"{current}.{i + 1}";

                if (File.Exists(src))
                {
                    if (File.Exists(next)) File.Delete(next);
                    File.Move(src, next);
                }
            }
        }
    }
}
