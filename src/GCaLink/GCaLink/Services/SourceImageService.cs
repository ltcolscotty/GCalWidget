using GCaLink.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;

namespace GCaLink.Services
{
    public sealed class SourceImageAssociation
    {
        public string Provider { get; set; } = string.Empty;
        public string SourceId { get; set; } = string.Empty;
        public string ManagedImageFileName { get; set; } = string.Empty;
        public DateTimeOffset UpdatedUtc { get; set; } = DateTimeOffset.UtcNow;
    }

    internal sealed class SourceImageService
    {
        private const string AssociationFileName = "SourceImageAssociations.json";
        private static readonly object SyncLock = new();

        public static SourceImageService Instance { get; } = new();
        public event EventHandler? SourceImagesChanged;

        private readonly string _managedImageDirectory;
        private readonly string _associationFilePath;
        private readonly Dictionary<string, SourceImageAssociation> _associations;
        private readonly FileSystemWatcher _associationWatcher;
        private readonly Timer _associationReloadTimer;

        public SourceImageService()
        {
            string appDataLocalPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string appDataLocalFolder = Path.Combine(appDataLocalPath, "GCWidget");
            _managedImageDirectory = Path.Combine(appDataLocalFolder, "SourceImages");
            _associationFilePath = Path.Combine(_managedImageDirectory, AssociationFileName);
            Directory.CreateDirectory(_managedImageDirectory);
            _associations = LoadAssociations();
            CleanupStaleAssociations();

            _associationReloadTimer = new Timer(_ => ReloadAssociations(), null, Timeout.Infinite, Timeout.Infinite);
            _associationWatcher = new FileSystemWatcher(_managedImageDirectory, AssociationFileName)
            {
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size
            };
            _associationWatcher.Changed += (_, _) => QueueAssociationReload();
            _associationWatcher.Created += (_, _) => QueueAssociationReload();
            _associationWatcher.Deleted += (_, _) => QueueAssociationReload();
            _associationWatcher.Renamed += (_, _) => QueueAssociationReload();
            _associationWatcher.EnableRaisingEvents = true;
            LoggerService.Log($"SourceImageService: Watching association file '{_associationFilePath}'.");
        }

        public string ManagedImageDirectory => _managedImageDirectory;

        public void ReloadAssociations()
        {
            LoggerService.Log($"SourceImageService: Reloading image associations from '{_associationFilePath}'.");
            Dictionary<string, SourceImageAssociation> latestAssociations = LoadAssociations();
            lock (SyncLock)
            {
                _associations.Clear();
                foreach (var association in latestAssociations)
                {
                    _associations[association.Key] = association.Value;
                }
            }

            CleanupStaleAssociations();
            LoggerService.Log($"SourceImageService: Reloaded {latestAssociations.Count} image associations.");
            SourceImagesChanged?.Invoke(this, EventArgs.Empty);
        }

        private void QueueAssociationReload()
        {
            LoggerService.Log("SourceImageService: Association file changed; debounce reload queued.");
            _associationReloadTimer.Change(150, Timeout.Infinite);
        }

        public IReadOnlyCollection<SourceImageAssociation> GetAllAssociations()
        {
            lock (SyncLock)
            {
                return new List<SourceImageAssociation>(_associations.Values);
            }
        }

        public bool IsSupportedImage(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                return false;
            }

            string extension = Path.GetExtension(filePath).Trim().ToLowerInvariant();
            return extension is ".png" or ".jpg" or ".jpeg";
        }

        public string? GetSourceImagePath(CalendarSourceIdentity source)
        {
            if (source.IsEmpty)
            {
                return null;
            }

            return GetSourceImagePath(source.Provider, source.SourceId);
        }

        public string? GetSourceImagePath(string provider, string sourceId)
        {
            if (string.IsNullOrWhiteSpace(provider) || string.IsNullOrWhiteSpace(sourceId))
            {
                return null;
            }

            string key = BuildKey(provider, sourceId);
            if (!_associations.TryGetValue(key, out SourceImageAssociation? association) ||
                string.IsNullOrWhiteSpace(association.ManagedImageFileName))
            {
                return null;
            }

            string fullPath = Path.Combine(_managedImageDirectory, association.ManagedImageFileName);
            if (!File.Exists(fullPath))
            {
                LoggerService.Log(
                    $"SourceImageService: Missing managed image for provider '{provider}' and source '{sourceId}'. Clearing stale association.",
                    LoggerStatusEnum.WARNING);
                RemoveSourceImage(provider, sourceId, persist: true);
                return null;
            }

            return fullPath;
        }

        public bool HasSourceImage(CalendarSourceIdentity source) => !string.IsNullOrWhiteSpace(GetSourceImagePath(source));

        public bool HasSourceImage(string provider, string sourceId) => !string.IsNullOrWhiteSpace(GetSourceImagePath(provider, sourceId));

        public bool TryAssignImageToSource(CalendarSourceIdentity source, string selectedImagePath, out string managedImagePath)
        {
            managedImagePath = string.Empty;
            if (source.IsEmpty)
            {
                LoggerService.Log("SourceImageService: Attempted to assign a source image without a valid source identity.", LoggerStatusEnum.ERROR);
                return false;
            }

            return TryAssignImageToSource(source.Provider, source.SourceId, selectedImagePath, out managedImagePath);
        }

        public bool TryAssignImageToSource(string provider, string sourceId, string selectedImagePath, out string managedImagePath)
        {
            managedImagePath = string.Empty;
            if (string.IsNullOrWhiteSpace(provider) || string.IsNullOrWhiteSpace(sourceId))
            {
                LoggerService.Log("SourceImageService: Attempted to assign an image without provider/source information.", LoggerStatusEnum.ERROR);
                return false;
            }

            if (string.IsNullOrWhiteSpace(selectedImagePath) || !File.Exists(selectedImagePath))
            {
                LoggerService.Log("SourceImageService: Selected image file does not exist or was not provided.", LoggerStatusEnum.ERROR);
                return false;
            }

            if (!IsSupportedImage(selectedImagePath))
            {
                LoggerService.Log(
                    $"SourceImageService: Unsupported image type for source '{provider}:{sourceId}' file '{selectedImagePath}'.",
                    LoggerStatusEnum.WARNING);
                return false;
            }

            try
            {
                string key = BuildKey(provider, sourceId);
                string extension = Path.GetExtension(selectedImagePath).ToLowerInvariant();
                string safeFileName = $"{Sanitize(provider)}-{Sanitize(sourceId)}-{Guid.NewGuid():N}{extension}";
                string destinationPath = Path.Combine(_managedImageDirectory, safeFileName);

                lock (SyncLock)
                {
                    RemoveManagedImageForAssociation(key, deleteFile: true);
                    File.Copy(selectedImagePath, destinationPath, overwrite: false);
                    _associations[key] = new SourceImageAssociation
                    {
                        Provider = provider.Trim(),
                        SourceId = sourceId.Trim(),
                        ManagedImageFileName = safeFileName,
                        UpdatedUtc = DateTimeOffset.UtcNow
                    };
                    SaveAssociations();
                    managedImagePath = destinationPath;
                }

                LoggerService.Log(
                    $"SourceImageService: Saved image customization for '{provider}:{sourceId}' to '{Path.GetFileName(managedImagePath)}'.");
                SourceImagesChanged?.Invoke(this, EventArgs.Empty);
                return true;
            }
            catch (Exception exception)
            {
                LoggerService.LogException($"SourceImageService: Failed to import image for provider '{provider}' and source '{sourceId}'.", exception);
                return false;
            }
        }

        public bool RemoveSourceImage(CalendarSourceIdentity source)
        {
            if (source.IsEmpty)
            {
                return false;
            }

            return RemoveSourceImage(source.Provider, source.SourceId);
        }

        public bool RemoveSourceImage(string provider, string sourceId)
        {
            return RemoveSourceImage(provider, sourceId, persist: true);
        }

        private bool RemoveSourceImage(string provider, string sourceId, bool persist)
        {
            if (string.IsNullOrWhiteSpace(provider) || string.IsNullOrWhiteSpace(sourceId))
            {
                return false;
            }

            string key = BuildKey(provider, sourceId);
            bool removed;
            lock (SyncLock)
            {
                removed = RemoveManagedImageForAssociation(key, deleteFile: true);
                if (!removed && persist)
                {
                    return false;
                }

                if (persist)
                {
                    SaveAssociations();
                }

            }

            if (removed)
            {
                SourceImagesChanged?.Invoke(this, EventArgs.Empty);
            }

            return true;
        }

        public int CleanupStaleAssociations()
        {
            int cleaned = 0;
            lock (SyncLock)
            {
                foreach (string key in new List<string>(_associations.Keys))
                {
                    SourceImageAssociation association = _associations[key];
                    string managedPath = Path.Combine(_managedImageDirectory, association.ManagedImageFileName);
                    if (File.Exists(managedPath))
                    {
                        continue;
                    }

                    LoggerService.Log(
                        $"SourceImageService: Missing managed image for provider '{association.Provider}' source '{association.SourceId}' at '{managedPath}'. Clearing stale association.",
                        LoggerStatusEnum.WARNING);
                    _associations.Remove(key);
                    cleaned++;
                }

                if (cleaned > 0)
                {
                    SaveAssociations();
                }
            }

            return cleaned;
        }

        private bool RemoveManagedImageForAssociation(string key, bool deleteFile)
        {
            if (!_associations.TryGetValue(key, out SourceImageAssociation? association))
            {
                return false;
            }

            if (deleteFile && !string.IsNullOrWhiteSpace(association.ManagedImageFileName))
            {
                string filePath = Path.Combine(_managedImageDirectory, association.ManagedImageFileName);
                if (File.Exists(filePath))
                {
                    try
                    {
                        File.Delete(filePath);
                    }
                    catch (Exception exception)
                    {
                        LoggerService.LogException($"SourceImageService: Failed to delete old image file '{filePath}'.", exception);
                    }
                }
            }

            _associations.Remove(key);
            return true;
        }

        private Dictionary<string, SourceImageAssociation> LoadAssociations()
        {
            if (!File.Exists(_associationFilePath))
            {
                return new Dictionary<string, SourceImageAssociation>(StringComparer.OrdinalIgnoreCase);
            }

            try
            {
                string json = File.ReadAllText(_associationFilePath);
                if (string.IsNullOrWhiteSpace(json))
                {
                    return new Dictionary<string, SourceImageAssociation>(StringComparer.OrdinalIgnoreCase);
                }

                var associations = JsonSerializer.Deserialize<Dictionary<string, SourceImageAssociation>>(json);
                if (associations == null)
                {
                    return new Dictionary<string, SourceImageAssociation>(StringComparer.OrdinalIgnoreCase);
                }

                foreach (var pair in associations)
                {
                    if (string.IsNullOrWhiteSpace(pair.Key) || pair.Value == null)
                    {
                        continue;
                    }
                }

                return new Dictionary<string, SourceImageAssociation>(associations, StringComparer.OrdinalIgnoreCase);
            }
            catch (Exception exception)
            {
                LoggerService.LogException($"SourceImageService: Failed to parse image associations file '{_associationFilePath}'.", exception);
                return new Dictionary<string, SourceImageAssociation>(StringComparer.OrdinalIgnoreCase);
            }
        }

        private void SaveAssociations()
        {
            try
            {
                var json = JsonSerializer.Serialize(_associations, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_associationFilePath, json);
            }
            catch (Exception exception)
            {
                LoggerService.LogException($"SourceImageService: Failed to save image associations to '{_associationFilePath}'.", exception);
            }
        }

        public static string BuildKey(string provider, string sourceId)
        {
            return $"{(provider ?? string.Empty).Trim()}:{(sourceId ?? string.Empty).Trim()}";
        }

        private static string Sanitize(string rawValue)
        {
            string sanitized = new string(rawValue
                .Trim()
                .Select(character => char.IsLetterOrDigit(character) || character == '_' || character == '-' ? character : '_')
                .ToArray());

            if (string.IsNullOrWhiteSpace(sanitized))
            {
                return "source";
            }

            return sanitized.Length > 80 ? sanitized[..80] : sanitized;
        }
    }
}
