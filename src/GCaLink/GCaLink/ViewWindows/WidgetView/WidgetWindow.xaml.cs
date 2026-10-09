using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
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
using GCaLink.Platform;
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
        public ObservableCollection<SourceCustomizationCard> GoogleSourceCards { get; } = new();
        public ObservableCollection<SourceCustomizationCard> CanvasSourceCards { get; } = new();

        private readonly RefreshSchedService _refreshSchedService;
        private readonly IDisposable _shutdownHandler;
        private bool _isUpdatingTransparencyControls;
        private bool _isUpdatingPrimaryViewControl;

        public WidgetWindow()
        {
            InitializeComponent();
            _ = SettingsRetriever.InitializeAsync();
            _refreshSchedService = new RefreshSchedService(
                TimeSpan.FromMinutes(SettingsRetriever.GetUpdateDurationMins()));
            _shutdownHandler = GCaLink.Platform.Win32Interop.RegisterShutdownHandler(
                WindowNative.GetWindowHandle(this),
                HandleShutdown);
            _isUpdatingTransparencyControls = true;
            WindowConfiguration.Configure(this, isWidgetViewActive: true);
            ViewWindowManager.SetPrimaryViewManagerActive(true);
            Closed += (_, _) =>
            {
                LoggerService.Log("WidgetWindow: Closed event received; publishing manager inactive state.");
                ViewWindowManager.SetPrimaryViewManagerActive(false);
                ViewWindowManager.PrimaryViewClosedByUser -= OnPrimaryViewClosedByUser;
                _shutdownHandler.Dispose();
                _ = _refreshSchedService.DisposeAsync();
            };
            Activated += (_, args) =>
            {
                if (args.WindowActivationState != WindowActivationState.Deactivated)
                {
                    ViewWindowManager.SetPrimaryViewManagerActive(true);
                }
            };

            _ = _refreshSchedService.RunAsync();

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
            EnablePrimaryView.IsChecked = SettingsRetriever.GetPrimaryViewEnabled();
            EnablePinnedView.IsChecked = SettingsRetriever.GetPinnedViewEnabled();
            PrimaryViewSelector.SelectedIndex = SettingsRetriever.GetPrimaryView() switch
            {
                PrimaryViewEnum.Todo => 0,
                PrimaryViewEnum.Day => 1,
                PrimaryViewEnum.Week => 2,
                _ => 0
            };
            int transparencyPercentage = SettingsRetriever.GetWindowTransparency();
            WindowTransparencySlider.Value = transparencyPercentage;
            WindowTransparencyInput.Text = transparencyPercentage.ToString();
            _isUpdatingTransparencyControls = false;
            ViewWindowManager.PrimaryViewClosedByUser += OnPrimaryViewClosedByUser;
            ApplyBackgroundType();
            ViewWindowManager.SyncWithSettings();
            _ = UpdateConnectionStatusAsync();
            _ = RefreshCustomizationCardsAsync();
        }

        internal void HandleShutdown()
        {
            LoggerService.Log("WidgetWindow: Windows shutdown confirmed; canceling in-progress refreshes.");
            EventAggService.RequestShutdown();
            _ = _refreshSchedService.DisposeAsync();
        }

        private async Task RefreshCustomizationCardsAsync()
        {
            GoogleSourceCards.Clear();
            CanvasSourceCards.Clear();

            foreach (var sourceCard in await BuildSourceCardsAsync("Google"))
            {
                GoogleSourceCards.Add(sourceCard);
            }

            foreach (var sourceCard in await BuildSourceCardsAsync("Canvas"))
            {
                CanvasSourceCards.Add(sourceCard);
            }

            GoogleSourceCardsList.ItemsSource = GoogleSourceCards;
            CanvasSourceCardsList.ItemsSource = CanvasSourceCards;
        }

        private async Task<List<SourceCustomizationCard>> BuildSourceCardsAsync(string provider)
        {
            var cards = new List<SourceCustomizationCard>();
            var knownAssociations = SourceImageService.Instance.GetAllAssociations();
            var seenKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (provider == GoogleCalService.ProviderName)
            {
                try
                {
                    foreach (GoogleCalendarSource calendar in await EventAggService.GetGoogleCalService().GetCalendarSourcesAsync())
                    {
                        CalendarSourceIdentity identity = CalendarSourceIdentity.CreateGoogle(calendar.AccountEmail, calendar.CalendarId);
                        if (!seenKeys.Add(identity.Key))
                        {
                            continue;
                        }

                        cards.Add(CreateSourceCustomizationCard(
                            identity,
                            $"{calendar.AccountEmail} - {calendar.DisplayName}"));
                    }
                }
                catch (Exception exception)
                {
                    LoggerService.LogException("WidgetWindow: Failed to load Google calendar sources for customization cards.", exception);
                }
            }

            if (File.Exists(SettingsRetriever.GetMainDataPath()))
            {
                Dictionary<IDHelper.EventID, CalEventDto> storedEvents = await EventAggService.ReadUpcomingEventsMessagePackAsync(SettingsRetriever.GetMainDataPath());
                foreach (var calendarEvent in storedEvents.Values)
                {
                    string eventProvider = calendarEvent.Provider ?? string.Empty;
                    bool isLegacyCanvasEvent = provider == "Canvas" &&
                        string.IsNullOrWhiteSpace(eventProvider) &&
                        !string.IsNullOrWhiteSpace(calendarEvent.LongSource);
                    if (!string.Equals(eventProvider, provider, StringComparison.OrdinalIgnoreCase) && !isLegacyCanvasEvent)
                    {
                        continue;
                    }

                    CalendarSourceIdentity identity;
                    string displayName;
                    if (provider == GoogleCalService.ProviderName)
                    {
                        if (string.IsNullOrWhiteSpace(calendarEvent.GoogleAccountEmail) ||
                            string.IsNullOrWhiteSpace(calendarEvent.CalendarId))
                        {
                            continue;
                        }

                        identity = CalendarSourceIdentity.CreateGoogle(
                            calendarEvent.GoogleAccountEmail,
                            calendarEvent.CalendarId);
                        displayName = $"{calendarEvent.GoogleAccountEmail} - {calendarEvent.Source}";
                    }
                    else
                    {
                        string className = calendarEvent.Source?.Trim() ?? string.Empty;
                        if (string.IsNullOrWhiteSpace(className))
                        {
                            continue;
                        }

                        identity = CalendarSourceIdentity.FromValues(provider, className);
                        displayName = className;
                    }

                    if (!seenKeys.Add(identity.Key))
                    {
                        continue;
                    }

                    cards.Add(CreateSourceCustomizationCard(identity, displayName));
                }
            }

            foreach (var association in knownAssociations)
            {
                if (!string.Equals(association.Provider, provider, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string key = SourceImageService.BuildKey(association.Provider, association.SourceId);
                if (!seenKeys.Add(key))
                {
                    continue;
                }

                var identity = CalendarSourceIdentity.FromValues(association.Provider, association.SourceId);
                string displayName = association.SourceId;
                if (provider == GoogleCalService.ProviderName &&
                    CalendarSourceIdentity.TryGetGoogleComponents(association.SourceId, out string accountEmail, out string calendarId))
                {
                    displayName = $"{accountEmail} - {calendarId}";
                }
                else if (provider == GoogleCalService.ProviderName && association.SourceId == GoogleCalService.PrimaryCalendarId)
                {
                    displayName = "Google Calendar (unscoped)";
                }

                cards.Add(CreateSourceCustomizationCard(identity, displayName));
            }

            return cards.OrderBy(card => card.DisplayName, StringComparer.OrdinalIgnoreCase).ToList();
        }

        private SourceCustomizationCard CreateSourceCustomizationCard(CalendarSourceIdentity identity, string? fallbackName = null)
        {
            string displayName = fallbackName ?? identity.SourceId;
            string imagePath = SourceImageService.Instance.GetSourceImagePath(identity) ?? string.Empty;
            string backgroundColor = "#3A3A3A";

            var config = SettingsRetriever.GetSourceConfigs().FirstOrDefault(pair =>
                pair.Key.Equals(identity.Key, StringComparison.OrdinalIgnoreCase) ||
                pair.Value.Source.Equals(identity.SourceId, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(config.Value?.BkgColor))
            {
                backgroundColor = config.Value.BkgColor;
            }

            var card = new SourceCustomizationCard
            {
                Provider = identity.Provider,
                SourceId = identity.SourceId,
                DisplayName = displayName,
                ImagePath = imagePath,
                HasImage = !string.IsNullOrWhiteSpace(imagePath),
                BackgroundColor = backgroundColor,
            };

            card.BackgroundBrush = CreateBackgroundBrush(card);
            card.StatusText = card.HasImage ? "Image configured" : "No image configured";
            return card;
        }

        private static Brush CreateBackgroundBrush(SourceCustomizationCard card)
        {
            if (!string.IsNullOrWhiteSpace(card.ImagePath) && File.Exists(card.ImagePath))
            {
                var imageBrush = new ImageBrush
                {
                    ImageSource = new BitmapImage(new Uri(card.ImagePath, UriKind.Absolute)),
                    Stretch = Stretch.UniformToFill,
                    AlignmentX = AlignmentX.Center,
                    AlignmentY = AlignmentY.Center,
                };

                return imageBrush;
            }

            if (TryParseColor(card.BackgroundColor, out var color))
            {
                return new SolidColorBrush(color);
            }

            return new SolidColorBrush(Windows.UI.Color.FromArgb(255, 58, 58, 58));
        }

        private static bool TryParseColor(string colorText, out Windows.UI.Color color)
        {
            color = Windows.UI.Color.FromArgb(255, 58, 58, 58);
            if (string.IsNullOrWhiteSpace(colorText))
            {
                return false;
            }

            try
            {
                var trimmed = colorText.Trim();
                if (trimmed.StartsWith("#"))
                {
                    trimmed = trimmed.Substring(1);
                }

                if (trimmed.Length == 6)
                {
                    trimmed = "FF" + trimmed;
                }

                if (trimmed.Length != 8)
                {
                    return false;
                }

                color = Windows.UI.Color.FromArgb(
                    byte.Parse(trimmed.Substring(0, 2), System.Globalization.NumberStyles.HexNumber),
                    byte.Parse(trimmed.Substring(2, 2), System.Globalization.NumberStyles.HexNumber),
                    byte.Parse(trimmed.Substring(4, 2), System.Globalization.NumberStyles.HexNumber),
                    byte.Parse(trimmed.Substring(6, 2), System.Globalization.NumberStyles.HexNumber));
                return true;
            }
            catch
            {
                return false;
            }
        }

        private async Task PickImageForCardAsync(SourceCustomizationCard card)
        {
            LoggerService.Log($"WidgetWindow: Opening image picker for source '{card.Provider}:{card.SourceId}'.");
            var picker = new FileOpenPicker();
            picker.ViewMode = PickerViewMode.Thumbnail;
            picker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            picker.FileTypeFilter.Add(".jpg");
            picker.FileTypeFilter.Add(".jpeg");
            picker.FileTypeFilter.Add(".png");

            var hwnd = WindowNative.GetWindowHandle(this);
            InitializeWithWindow.Initialize(picker, hwnd);

            StorageFile file = await picker.PickSingleFileAsync();
            if (file == null)
            {
                LoggerService.Log($"WidgetWindow: Image picker cancelled for source '{card.Provider}:{card.SourceId}'.");
                return;
            }

            bool assigned = SourceImageService.Instance.TryAssignImageToSource(
                card.Provider,
                card.SourceId,
                file.Path,
                out string managedPath);

            if (assigned)
            {
                LoggerService.Log($"WidgetWindow: Image customization saved for source '{card.Provider}:{card.SourceId}'.");
                await RefreshCustomizationCardsAsync();
            }
            else
            {
                LoggerService.Log(
                    $"WidgetWindow: Image customization failed for source '{card.Provider}:{card.SourceId}'.",
                    LoggerStatusEnum.WARNING);
            }
        }

        private async Task RemoveImageForCardAsync(SourceCustomizationCard card)
        {
            bool removed = SourceImageService.Instance.RemoveSourceImage(card.Provider, card.SourceId);
            if (removed)
            {
                await RefreshCustomizationCardsAsync();
            }
        }

        private async void ChooseBackgroundImageClick(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button button && button.DataContext is SourceCustomizationCard card)
                {
                    await PickImageForCardAsync(card);
                    return;
                }

                await PickImageForCardAsync(new SourceCustomizationCard
                {
                    Provider = "Google",
                    SourceId = GoogleCalService.PrimaryCalendarId,
                    DisplayName = "Google Calendar"
                });
            }
            catch (Exception exception)
            {
                LoggerService.LogException("WidgetWindow: Image picker or customization failed.", exception);
            }
        }

        private async void RemoveBackgroundImageClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is SourceCustomizationCard card)
            {
                await RemoveImageForCardAsync(card);
            }
        }

        public sealed class SourceCustomizationCard
        {
            public string Provider { get; set; } = string.Empty;
            public string SourceId { get; set; } = string.Empty;
            public string DisplayName { get; set; } = string.Empty;
            public string ImagePath { get; set; } = string.Empty;
            public bool HasImage { get; set; }
            public string BackgroundColor { get; set; } = "#3A3A3A";
            public string StatusText { get; set; } = "No image configured";
            public Brush BackgroundBrush { get; set; } = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 58, 58, 58));
        }

        private void BkgStyleChanged(object sender, SelectionChangedEventArgs e)
        {
            SaveBackgroundType();
            ApplyBackgroundType();
        }

        private void WindowTransparencySliderChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (_isUpdatingTransparencyControls)
            {
                return;
            }

            int transparencyPercentage = (int)Math.Round(e.NewValue);
            _isUpdatingTransparencyControls = true;
            WindowTransparencyInput.Text = transparencyPercentage.ToString();
            _isUpdatingTransparencyControls = false;
            SettingsRetriever.SetWindowTransparency(transparencyPercentage);
        }

        private void WindowTransparencyInputChanged(object sender, TextChangedEventArgs e)
        {
            if (_isUpdatingTransparencyControls ||
                !int.TryParse(WindowTransparencyInput.Text, out int transparencyPercentage) ||
                transparencyPercentage is < 0 or > 100)
            {
                return;
            }

            WindowTransparencySlider.Value = transparencyPercentage;
        }

        private void WindowTransparencyInputLostFocus(object sender, RoutedEventArgs e)
        {
            if (_isUpdatingTransparencyControls)
            {
                return;
            }

            int transparencyPercentage = SettingsRetriever.GetWindowTransparency();
            _isUpdatingTransparencyControls = true;
            WindowTransparencyInput.Text = transparencyPercentage.ToString();
            WindowTransparencySlider.Value = transparencyPercentage;
            _isUpdatingTransparencyControls = false;
        }

        private async void GoogleSIClick(object sender, RoutedEventArgs e)
        {
            await GoogleSignInAsync();
        }

        private async void GoogleSignOutClick(object sender, RoutedEventArgs e)
        {
            await EventAggService.GetGoogleCalService().SignOutAsync();
            SettingsRetriever.SetGoogleEnabled(false);
            EnableGoogle.IsChecked = false;
            await UpdateConnectionStatusAsync(false);
        }

        private async void GoogleSwitchAccountClick(object sender, RoutedEventArgs e)
        {
            await EventAggService.GetGoogleCalService().SignOutAsync();
            SettingsRetriever.SetGoogleEnabled(false);
            EnableGoogle.IsChecked = false;
            await GoogleSignInAsync();
        }

        private async Task GoogleSignInAsync()
        {
            GoogleSI.IsEnabled = false;
            GoogleConnectionStatus.Text = "Waiting for Google sign-in...";
            try
            {
                bool connected = await EventAggService.GetGoogleCalService().AuthorizeAsync();
                if (connected)
                {
                    SettingsRetriever.SetGoogleEnabled(true);
                    EnableGoogle.IsChecked = true;
                    await EventAggService.ReloadGoogleServiceAsync();
                }

                await UpdateConnectionStatusAsync(connected);
                await UpdateRefreshButton();
            }
            finally
            {
                GoogleSI.IsEnabled = true;
            }
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
                if (response != true)
                {
                    LoggerService.Log(
                        $"WidgetWindow.RefreshCanvasSources: Canvas refresh did not complete (result: {response?.ToString() ?? "null"}).",
                        response == false ? LoggerStatusEnum.WARNING : LoggerStatusEnum.ERROR);
                }
                CanvasSaveStatus.Text = response == true
                    ? "Canvas events refreshed."
                    : "Canvas events could not be refreshed.";
            }
            catch (OperationCanceledException) when (EventAggService.IsShutdownRequested)
            {
                LoggerService.Log("WidgetWindow: Canvas refresh canceled because Windows is shutting down.");
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
                    LoggerService.Log(
                        $"WidgetWindow.RefreshGoogleSources: Could not find '{mainDataPath}'. Creating a default file.",
                        LoggerStatusEnum.WARNING);
                    await EventAggService.WriteUpcomingEventsMessagePackAsync(mainDataPath);
                }

                bool? response = await EventAggService.RefreshGoogle();
                GoogleConnectionStatus.Text = response == true
                    ? "Google Calendar events refreshed."
                    : "Google Calendar events could not be refreshed.";
            }
            catch (OperationCanceledException) when (EventAggService.IsShutdownRequested)
            {
                LoggerService.Log("WidgetWindow: Google refresh canceled because Windows is shutting down.");
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
            catch (OperationCanceledException) when (EventAggService.IsShutdownRequested)
            {
                LoggerService.Log("WidgetWindow: Source refresh canceled because Windows is shutting down.");
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

        private async Task UpdateConnectionStatusAsync(bool? connectedOverride = null)
        {
            bool connected = connectedOverride ?? await EventAggService.GetGoogleCalService().IsAccountActiveAsync();
            GoogleConnectionStatus.Text = connected ? "Connected to Google Calendar." : "Not connected";
            GoogleSignedOutPanel.Visibility = connected ? Visibility.Collapsed : Visibility.Visible;
            GoogleAccountCard.Visibility = connected ? Visibility.Visible : Visibility.Collapsed;
            RefreshGoogleSourcesButton.Visibility = connected && SettingsRetriever.GetGoogleEnabled()
                ? Visibility.Visible
                : Visibility.Collapsed;

            if (connected)
            {
                GoogleAccountProfile? profile = await EventAggService.GetGoogleCalService().GetAccountProfileAsync();
                if (profile != null)
                {
                    GoogleProfileNameText.Text = string.IsNullOrWhiteSpace(profile.Name) ? profile.Email : profile.Name;
                    GoogleProfileEmailText.Text = profile.Email;
                    if (Uri.TryCreate(profile.PictureUrl, UriKind.Absolute, out Uri? pictureUri))
                        GoogleProfileImage.Source = new BitmapImage(pictureUri);
                }
            }
        }

        private void ApplyBackgroundType()
        {
            WindowConfiguration.ApplyBackground(this);
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

            PrimaryViewEnum selectedView = PrimaryViewSelector.SelectedIndex switch
            {
                0 => PrimaryViewEnum.Todo,
                1 => PrimaryViewEnum.Day,
                2 => PrimaryViewEnum.Week,
                _ => PrimaryViewEnum.Todo
            };
            SettingsRetriever.SetPrimaryView(selectedView);
            if (SettingsRetriever.GetPrimaryViewEnabled())
            {
                ViewWindowManager.SyncWithSettings(replacePrimaryView: true);
            }
        }

        private void PrimaryViewEnabled(object sender, RoutedEventArgs e)
        {
            if (_isUpdatingPrimaryViewControl)
            {
                return;
            }

            SettingsRetriever.SetPrimaryViewEnabled(true);
            ViewWindowManager.SyncWithSettings();
        }

        private void PrimaryViewDisabled(object sender, RoutedEventArgs e)
        {
            if (_isUpdatingPrimaryViewControl)
            {
                return;
            }

            SettingsRetriever.SetPrimaryViewEnabled(false);
            ViewWindowManager.SyncWithSettings();
        }

        private void OnPrimaryViewClosedByUser()
        {
            SettingsRetriever.SetPrimaryViewEnabled(false);
            _isUpdatingPrimaryViewControl = true;
            EnablePrimaryView.IsChecked = false;
            _isUpdatingPrimaryViewControl = false;
        }

        private void PinnedViewEnabled(object sender, RoutedEventArgs e)
        {
            SettingsRetriever.SetPinnedViewEnabled(true);
            ViewWindowManager.SyncWithSettings();
        }

        private void PinnedViewDisabled(object sender, RoutedEventArgs e)
        {
            SettingsRetriever.SetPinnedViewEnabled(false);
            ViewWindowManager.SyncWithSettings();
        }
    }
}
