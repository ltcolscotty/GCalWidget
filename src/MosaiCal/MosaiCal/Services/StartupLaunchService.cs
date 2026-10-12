using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace MosaiCal.Services;

internal static class StartupLaunchService
{
    public const string StartupArgument = "--startup";

    private const string StartupScriptFileName = "MosaiCalStartup.vbs";
    private const string StartupScript = """"
        Set shell = CreateObject("WScript.Shell")
        command = "powershell.exe -NoProfile -WindowStyle Hidden -Command ""$package = Get-AppxPackage -Name 'MosaiCal' | Select-Object -First 1; if ($null -eq $package) { exit 1 }; Start-Process -FilePath (Join-Path $package.InstallLocation 'MosaiCal.exe') -ArgumentList '--startup'"""
        shell.Run command, 0, False
        """";

    public static async Task RequestConsentIfNeededAsync(Window owner)
    {
        try
        {
            if (SettingsRetriever.GetStartupAtLoginConsent() is null)
            {
                if (owner.Content is not FrameworkElement content || content.XamlRoot is null)
                {
                    throw new InvalidOperationException("The configuration window is not ready to show the startup consent dialog.");
                }

                ContentDialog dialog = new()
                {
                    Title = "Start MosaiCal at sign-in?",
                    Content = "When enabled, the configured calendar view will open automatically when you sign in.",
                    PrimaryButtonText = "Allow",
                    CloseButtonText = "Not now",
                    DefaultButton = ContentDialogButton.Primary,
                    XamlRoot = content.XamlRoot
                };

                ContentDialogResult result = await dialog.ShowAsync();
                bool consent = result == ContentDialogResult.Primary;
                SettingsRetriever.SetStartupAtLoginConsent(consent);
                LoggerService.Log($"StartupLaunchService: User startup consent recorded as {consent}.");
            }

            SyncWithSettings();
        }
        catch (Exception exception)
        {
            LoggerService.LogException("StartupLaunchService: Failed to request startup consent.", exception);
        }
    }

    public static void SyncWithSettings()
    {
        string startupFolder = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
        string startupScriptPath = Path.Combine(startupFolder, StartupScriptFileName);

        try
        {
            if (SettingsRetriever.GetStartupAtLoginConsent() != true ||
                !SettingsRetriever.GetPrimaryViewEnabled())
            {
                if (File.Exists(startupScriptPath))
                {
                    File.Delete(startupScriptPath);
                }

                LoggerService.Log(
                    "StartupLaunchService: Startup launcher removed because startup consent is absent or the calendar view is disabled.");
                return;
            }

            Directory.CreateDirectory(startupFolder);
            File.WriteAllText(startupScriptPath, StartupScript, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            LoggerService.Log($"StartupLaunchService: Startup launcher synchronized at '{startupScriptPath}'.");
        }
        catch (Exception exception)
        {
            LoggerService.LogException("StartupLaunchService: Failed to synchronize the startup launcher.", exception);
        }
    }
}

