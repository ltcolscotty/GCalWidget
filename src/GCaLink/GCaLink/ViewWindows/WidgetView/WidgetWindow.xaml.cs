using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI;
using GCaLink.Models;
using GCaLink.Services;
using GCaLink.ViewWindows;
using Windows.ApplicationModel.UserDataTasks;
using System.Security.Cryptography.X509Certificates;
using System.Diagnostics.Contracts;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace GCaLink.ViewWindows.WidgetView
{
    // <summary>
    // An empty window that can be used on its own or navigated to within a Frame.
    // </summary>
    public sealed partial class WidgetWindow : Window
    {
        public WidgetWindow()
        {
            InitializeComponent();
            WindowConfiguration.Configure(this, isWidgetViewActive: true);

            SettingsRetriever.InitializeAsync();

            BkgStyleRadioSettings.SelectedIndex = SettingsRetriever.GetBackgroundType() switch
            {
                BackgroundTypeEnum.Solid => 0,
                BackgroundTypeEnum.Mica => 1,
                BackgroundTypeEnum.Acrylic => 2,
                _ => 0
            };

            CanvasCalLinkInput.Text = SettingsRetriever.GetCanvasICSLink();
            EnableGoogle.IsChecked = SettingsRetriever.GetGoogleEnabled();
            EnableCanvas.IsChecked = SettingsRetriever.GetCanvasEnabled();
            EnablePinnedView.IsChecked = SettingsRetriever.GetPinnedViewEnabled();
            PrimaryViewSelector.SelectedIndex = SettingsRetriever.GetPrimaryView() switch
            {
                PrimaryViewEnum.Day => 0,
                PrimaryViewEnum.Week => 2,
                _ => 1
            };
            ApplyBackgroundType();
            _ = UpdateConnectionStatusAsync();
        }

        // Ideally a 1.0 feature, not of focus right now
        private async void ChooseBackgroundImageClick(object sender, RoutedEventArgs e)
        {
            var picker = new FileOpenPicker();
            picker.ViewMode = PickerViewMode.Thumbnail;
            picker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            picker.FileTypeFilter.Add(".jpg");
            picker.FileTypeFilter.Add(".jpeg");
            picker.FileTypeFilter.Add(".png");

            var hwnd = WindowNative.GetWindowHandle(App.Current);
            InitializeWithWindow.Initialize(picker, hwnd);

            StorageFile file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                // make copy of file, rename it, and store it in SettingsRetriever.GetImageDataFolder()
                // will probably need to add file management to delete unused images
                // cache up to n files in the folder in the case the user wants quick access?
            }
        }

        private void BkgStyleChanged(object sender, SelectionChangedEventArgs e)
        {
            SaveBackgroundType();
            ApplyBackgroundType();
        }

        private async void GoogleSIClick(object sender, RoutedEventArgs e)
        {
            GoogleSI.IsEnabled = false;
            GoogleConnectionStatus.Text = "Waiting for Google sign-in...";
            try
            {
                bool connected = await EventAggService.GetGoogleCalService().AuthorizeAsync();
                GoogleConnectionStatus.Text = connected
                    ? "Connected to Google Calendar."
                    : "Google sign-in did not complete.";

                if (connected)
                {
                    SettingsRetriever.SetGoogleEnabled(true);
                    EnableGoogle.IsChecked = true;
                    await EventAggService.ReloadGoogleServiceAsync();
                    await UpdateRefreshButton();
                }
            }
            finally
            {
                GoogleSI.IsEnabled = true;
            }
        }

        private void MainSaveClick(object sender, RoutedEventArgs e)
        {
            SaveBackgroundType();
        }

        private void SaveBackgroundType()
        {
            if (BkgStyleRadioSettings.SelectedItem is not string selectedStyle)
            {
                return;
            }

            BackgroundTypeEnum backgroundType = selectedStyle switch
            {
                "Mica" => BackgroundTypeEnum.Mica,
                "Acrylic" => BackgroundTypeEnum.Acrylic,
                _ => BackgroundTypeEnum.Solid
            };

            SettingsRetriever.SetBackgroundType(backgroundType);
        }
        
        private async void CanvasSaveClick(object sender, RoutedEventArgs e)
        {
            bool response = SettingsRetriever.SetCanvasICSLink(CanvasCalLinkInput.Text);
            if (!response)
            {
                // Notification - Unsuccessful
                CanvasSaveStatus.Text = "Enter a valid calendar URL.";
                return;
            }

            CanvasSaveStatus.Text = "Canvas calendar link saved.";
            await EventAggService.ReloadSourcesAsync();

        }

        private async void RefreshCanvasSources(object sender, RoutedEventArgs e)
        {
            try
            {
                bool? response = await EventAggService.RefreshCanvas();
                CanvasSaveStatus.Text = response == true
                    ? "Canvas events refreshed."
                    : "Canvas events could not be refreshed.";
            }
            catch (Exception exception)
            {
                LoggerService.LogException("WidgetWindow.RefreshCanvasSources", exception);
                CanvasSaveStatus.Text = "Canvas refresh failed. See GCWLogs.txt for details.";
            }
        }
        private async void RefreshGoogleSources(object sender, RoutedEventArgs e)
        {
            try
            {
                string mainDataPath = SettingsRetriever.GetMainDataPath();
                if (!File.Exists(mainDataPath))
                {
                    LoggerService.LogWarning(
                        $"WidgetWindow.RefreshGoogleSources: Could not find '{mainDataPath}'. Creating a default file.",
                        LoggerStatusEnum.WARNING);
                    await EventAggService.WriteUpcomingEventsMessagePackAsync(mainDataPath);
                }

                bool? response = await EventAggService.RefreshGoogle();
                GoogleConnectionStatus.Text = response == true
                    ? "Google Calendar events refreshed."
                    : "Google Calendar events could not be refreshed.";
            }
            catch (Exception exception)
            {
                LoggerService.LogException("WidgetWindow.RefreshGoogleSources", exception);
                GoogleConnectionStatus.Text = "Google refresh failed. See GCWLogs.txt for details.";
            }
        }

        private async void RefreshAll(object sender, RoutedEventArgs e)
        {
            try
            {
                await EventAggService.WriteUpcomingEventsMessagePackAsync(null);
            }
            catch (Exception exception)
            {
                LoggerService.LogException("WidgetWindow.RefreshAll", exception);
            }
        }
        
        private async Task UpdateRefreshButton()
        {
            GoogleCalService GCS = EventAggService.GetGoogleCalService();
            Dictionary<string, bool> sources = await SettingsRetriever.GetActiveSources(GCS);
            RefreshGoogleSourcesButton.Visibility = sources["google"]
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private async Task UpdateConnectionStatusAsync()
        {
            bool connected = await EventAggService.GetGoogleCalService().IsAccountActiveAsync();
            GoogleConnectionStatus.Text = connected ? "Connected to Google Calendar." : "Not connected";
            RefreshGoogleSourcesButton.Visibility = connected && SettingsRetriever.GetGoogleEnabled()
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private void ApplyBackgroundType()
        {
            SystemBackdrop = SettingsRetriever.GetBackgroundType() switch
            {
                BackgroundTypeEnum.Mica => new MicaBackdrop(),
                BackgroundTypeEnum.Acrylic => new DesktopAcrylicBackdrop(),
                _ => null
            };
        }

        private void GoogleEnabled(object sender, RoutedEventArgs e)
        {
            SettingsRetriever.SetGoogleEnabled(true);
        }
        private void GoogleDisabled(object sender, RoutedEventArgs e)
        {
            SettingsRetriever.SetGoogleEnabled(false);
        }

        private void CanvasEnabled(object sender, RoutedEventArgs e)
        {
            SettingsRetriever.SetCanvasEnabled(true);
        }

        private void CanvasDisabled(object sender, RoutedEventArgs e)
        {
            SettingsRetriever.SetCanvasEnabled(false);
        }

        private void PrimaryViewChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PrimaryViewSelector.SelectedIndex is < 0 or > 2)
            {
                return;
            }

            SettingsRetriever.SetPrimaryView((PrimaryViewEnum)PrimaryViewSelector.SelectedIndex);
        }

        private void PinnedViewEnabled(object sender, RoutedEventArgs e)
        {
            SettingsRetriever.SetPinnedViewEnabled(true);
        }

        private void PinnedViewDisabled(object sender, RoutedEventArgs e)
        {
            SettingsRetriever.SetPinnedViewEnabled(false);
        }
    }
}
