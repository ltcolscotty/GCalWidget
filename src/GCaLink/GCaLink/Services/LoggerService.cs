using GCaLink.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GCaLink.Services
{
    public enum LoggerStatus
    {
        INFO,
        WARNING,
        EXCEPTION,
        ERROR,
    }

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

            if (File.Exists(logFilePath))
            {
                return;
            }

            using var _ = File.Create(logFilePath);
        }

        public static void LogWarning(string warningText, LoggerStatus statusType=LoggerStatus.INFO)
        {
            try
            {
                lock (lockObj)
                {
                    FileInfo? info = new FileInfo(logFilePath);
                    string logType = "";
                    if (info.Exists && info.Length >= MaxFileSizeBytes)
                    {
                        Rotate();
                    }

                    if (statusType == LoggerStatus.ERROR)
                    {
                        logType = "[ERROR]";
                    }
                    else if (statusType == LoggerStatus.EXCEPTION)
                    {
                        logType = "[EXCEPTION]";
                    }
                    else if (statusType == LoggerStatus.WARNING)
                    {
                        logType = "[WARNING]";
                    }
                    else if (statusType == LoggerStatus.INFO)
                    {
                        logType = "[INFO]";
                    }
                    else
                    {
                        logType = "[UNDEFINED]";
                        File.AppendAllText(logFilePath, $"{DateTimeOffset.UtcNow:O} [LOGGERWARNING] invalid enum type recieved {Environment.NewLine}");
                    }

                    File.AppendAllText(logFilePath, $"{DateTimeOffset.UtcNow:O} {logType} {warningText}{Environment.NewLine}");
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

                if (!File.Exists(src))
                {
                    continue;
                }

                if (File.Exists(next)) File.Delete(next);

                File.Move(src, next);
            }
        }
    }
}
