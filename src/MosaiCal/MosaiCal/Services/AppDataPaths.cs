using System;
using System.IO;
using Windows.ApplicationModel;
using Windows.Storage;

namespace MosaiCal.Services;

internal static class AppDataPaths
{
    private static readonly string legacyLocalFolderPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "MosaiCal");

    static AppDataPaths()
    {
        try
        {
            _ = Package.Current;
            IsPackaged = true;
        }
        catch (InvalidOperationException)
        {
            IsPackaged = false;
        }

        LocalFolderPath = IsPackaged
            ? ApplicationData.Current.LocalFolder.Path
            : legacyLocalFolderPath;
    }

    public static string LocalFolderPath { get; }

    public static bool IsPackaged { get; }

    public static void EnsureCreated()
    {
        EnsureWritableDirectory(LocalFolderPath);
        EnsureWritableDirectory(Path.Combine(LocalFolderPath, "SourceImages"));
    }

    private static void EnsureWritableDirectory(string directoryPath)
    {
        try
        {
            Directory.CreateDirectory(directoryPath);

            string probePath = Path.Combine(directoryPath, $".write-test-{Guid.NewGuid():N}");
            using (new FileStream(probePath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
            }
            File.Delete(probePath);
        }
        catch (Exception exception) when (
            exception is IOException ||
            exception is UnauthorizedAccessException ||
            exception is ArgumentException ||
            exception is NotSupportedException)
        {
            throw new IOException(
                $"MosaiCal could not create and write to its required data directory '{directoryPath}'.",
                exception);
        }
    }
}
