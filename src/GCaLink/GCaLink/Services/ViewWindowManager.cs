using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using GCaLink.Models;
using GCaLink.Platform;
using GCaLink.ViewWindows.DayView;
using GCaLink.ViewWindows.PinnedView;
using GCaLink.ViewWindows.TodoView;
using GCaLink.ViewWindows.WeekView;
using GCaLink.ViewWindows;
using Microsoft.UI.Xaml;

namespace GCaLink.Services
{
    internal static class ViewWindowManager
    {
        private static volatile Window? primaryViewWindow;
        private static PrimaryViewEnum? primaryViewType;
        private static PinnedViewWindow? pinnedViewWindow;
        private static volatile bool primaryViewManagerActive;
        private static readonly string managerStatePath = Path.Combine(
            Path.GetDirectoryName(SettingsRetriever.GetMainDataPath())!,
            "GCWWidgetManagerState");
        private static readonly string primaryViewStatePath = Path.Combine(
            Path.GetDirectoryName(SettingsRetriever.GetMainDataPath())!,
            "GCWPrimaryViewState");
        private static readonly object managerStateLock = new();
        private static readonly Timer managerStateDebounceTimer = new(
            _ => ApplyPublishedManagerState(),
            null,
            Timeout.Infinite,
            Timeout.Infinite);
        private static FileSystemWatcher? managerStateWatcher;
        private static readonly object primaryViewStateLock = new();
        private static readonly Timer primaryViewStateDebounceTimer = new(
            _ => ApplyPublishedPrimaryViewState(),
            null,
            Timeout.Infinite,
            Timeout.Infinite);
        private static FileSystemWatcher? primaryViewStateWatcher;

        private sealed record PublishedPrimaryViewState(int ProcessId, bool Enabled, PrimaryViewEnum View);

        public static event Action? PrimaryViewClosedByUser;

        static ViewWindowManager()
        {
            string directory = Path.GetDirectoryName(managerStatePath)!;
            managerStateWatcher = new FileSystemWatcher(directory, Path.GetFileName(managerStatePath))
            {
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size
            };
            managerStateWatcher.Changed += (_, _) => QueueManagerStateUpdate();
            managerStateWatcher.Created += (_, _) => QueueManagerStateUpdate();
            managerStateWatcher.Renamed += (_, _) => QueueManagerStateUpdate();
            managerStateWatcher.EnableRaisingEvents = true;

            primaryViewStateWatcher = new FileSystemWatcher(directory, Path.GetFileName(primaryViewStatePath))
            {
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size
            };
            primaryViewStateWatcher.Changed += (_, _) => QueuePrimaryViewStateUpdate();
            primaryViewStateWatcher.Created += (_, _) => QueuePrimaryViewStateUpdate();
            primaryViewStateWatcher.Renamed += (_, _) => QueuePrimaryViewStateUpdate();
            primaryViewStateWatcher.EnableRaisingEvents = true;
        }

        public static void SetPrimaryViewManagerActive(bool isActive)
        {
            bool stateChanged = primaryViewManagerActive != isActive;
            LoggerService.Log(
                $"ViewWindowManager: Manager activation requested; active={isActive}, previous={primaryViewManagerActive}, stateChanged={stateChanged}, hasLocalPrimaryView={primaryViewWindow is not null}.");
            primaryViewManagerActive = isActive;
            if (stateChanged)
            {
                PublishManagerState(isActive);
            }

            if (primaryViewWindow is not null)
            {
                DesktopWidgetWindowBehavior.SetWidgetManagerActive(primaryViewWindow, isActive);
                if (isActive && stateChanged)
                {
                    _ = RefreshViewCustomizationsAsync();
                }
            }
            else
            {
                bool foundExistingView = SetExistingPrimaryViewMode(isActive);
                if (isActive && stateChanged && foundExistingView)
                {
                    EventAggService.NotifyViewsChanged();
                }
            }
        }

        private static void PublishManagerState(bool isActive)
        {
            try
            {
                string temporaryPath = $"{managerStatePath}.{Environment.ProcessId}.tmp";
                File.WriteAllText(temporaryPath, isActive.ToString());
                File.Move(temporaryPath, managerStatePath, overwrite: true);
                LoggerService.Log($"ViewWindowManager: Published manager state active={isActive} to '{managerStatePath}'.");
            }
            catch (Exception exception)
            {
                LoggerService.LogException("ViewWindowManager.PublishManagerState", exception);
            }
        }

        private static void QueueManagerStateUpdate()
        {
            lock (managerStateLock)
            {
                managerStateDebounceTimer.Change(150, Timeout.Infinite);
            }
        }

        private static void ApplyPublishedManagerState()
        {
            bool isActive;
            try
            {
                if (!bool.TryParse(File.ReadAllText(managerStatePath), out isActive))
                {
                    return;
                }
            }
            catch (IOException)
            {
                return;
            }

            bool stateChanged = isActive != primaryViewManagerActive;
            LoggerService.Log(
                $"ViewWindowManager: Received manager state active={isActive}, previous={primaryViewManagerActive}, stateChanged={stateChanged}, hasLocalPrimaryView={primaryViewWindow is not null}.");
            if (!stateChanged && !isActive)
            {
                return;
            }

            primaryViewManagerActive = isActive;
            Window? window = primaryViewWindow;
            if (window is not null)
            {
                bool enqueued = window.DispatcherQueue.TryEnqueue(() =>
                {
                    if (!ReferenceEquals(primaryViewWindow, window) ||
                        primaryViewManagerActive != isActive)
                    {
                        LoggerService.Log(
                            $"ViewWindowManager: Skipped stale manager-state callback active={isActive}, currentActive={primaryViewManagerActive}, sameWindow={ReferenceEquals(primaryViewWindow, window)}.",
                            LoggerStatusEnum.WARNING);
                        return;
                    }

                    LoggerService.Log($"ViewWindowManager: Applying manager mode active={isActive} to the local primary view.");
                    DesktopWidgetWindowBehavior.SetWidgetManagerActive(window, isActive);
                    if (isActive)
                    {
                        _ = RefreshViewCustomizationsAsync();
                    }
                });
                LoggerService.Log(
                    $"ViewWindowManager: View-mode update enqueued={enqueued} for active={isActive}.",
                    enqueued ? LoggerStatusEnum.INFO : LoggerStatusEnum.WARNING);
            }
            else
            {
                LoggerService.Log("ViewWindowManager: No local primary view was available for the published manager-state update.", LoggerStatusEnum.WARNING);
            }
        }

        private static async Task RefreshViewCustomizationsAsync()
        {
            try
            {
                LoggerService.Log("ViewWindowManager: Reloading persisted calendar customizations.");
                await SettingsRetriever.InitializeAsync(forceRefresh: true, notifyViews: false);
                SourceImageService.Instance.ReloadAssociations();
                LoggerService.Log("ViewWindowManager: Persisted calendar customization reload completed.");
            }
            catch (Exception exception)
            {
                LoggerService.LogException("ViewWindowManager: Failed to refresh calendar customizations.", exception);
            }
        }

        public static void SyncWithSettings(bool replacePrimaryView = false)
        {
            PublishPrimaryViewState(SettingsRetriever.GetPrimaryViewEnabled(), SettingsRetriever.GetPrimaryView());
            LoggerService.Log(
                $"ViewWindowManager: Sync settings; primaryEnabled={SettingsRetriever.GetPrimaryViewEnabled()}, primaryView={SettingsRetriever.GetPrimaryView()}, replace={replacePrimaryView}, hasLocalPrimaryView={primaryViewWindow is not null}.");
            if (SettingsRetriever.GetPrimaryViewEnabled())
            {
                OpenPrimaryView(replacePrimaryView);
            }
            else
            {
                ClosePrimaryView("primary view disabled in settings");
            }

            if (SettingsRetriever.GetPinnedViewEnabled())
            {
                OpenPinnedView();
            }
            else
            {
                ClosePinnedView();
            }
        }

        private static void OpenPrimaryView(bool replaceExisting)
        {
            PrimaryViewEnum selectedView = SettingsRetriever.GetPrimaryView();
            LoggerService.Log(
                $"ViewWindowManager: Open primary requested; selected={selectedView}, replace={replaceExisting}, current={primaryViewType}, hasLocalPrimaryView={primaryViewWindow is not null}.");
            if (primaryViewWindow is not null &&
                (!replaceExisting || primaryViewType == selectedView))
            {
                if (primaryViewManagerActive)
                {
                    primaryViewWindow.Activate();
                }
                return;
            }

            ClosePrimaryView($"replacing primary view with {selectedView}");
            if (TryActivateExistingPrimaryView())
            {
                return;
            }

            CreatePrimaryViewWindow(selectedView);
        }

        private static void ReplaceOwnedPrimaryView(PrimaryViewEnum selectedView)
        {
            LoggerService.Log($"ViewWindowManager: Replacing owned primary view directly with {selectedView}.");
            ClosePrimaryView($"replacing owned primary view with {selectedView}");
            CreatePrimaryViewWindow(selectedView);
        }

        private static void CreatePrimaryViewWindow(PrimaryViewEnum selectedView)
        {
            primaryViewWindow = selectedView switch
            {
                PrimaryViewEnum.Day => new DayViewWindow(),
                PrimaryViewEnum.Week => new WeekViewWindow(),
                _ => new TodoViewWindow()
            };
            DesktopWidgetWindowBehavior.SetWidgetManagerActive(primaryViewWindow, primaryViewManagerActive);
            primaryViewType = selectedView;
            primaryViewWindow.Closed += PrimaryViewClosed;
            if (primaryViewManagerActive)
            {
                primaryViewWindow.Activate();
            }
        }

        private static void ClosePrimaryView(string reason)
        {
            Window? window = primaryViewWindow;
            LoggerService.Log(
                $"ViewWindowManager: Close primary requested; reason='{reason}', hasLocalPrimaryView={window is not null}.",
                window is null ? LoggerStatusEnum.INFO : LoggerStatusEnum.WARNING);
            primaryViewWindow = null;
            primaryViewType = null;
            window?.Close();
        }

        private static void PublishPrimaryViewState(bool enabled, PrimaryViewEnum view)
        {
            try
            {
                var state = new PublishedPrimaryViewState(Environment.ProcessId, enabled, view);
                string temporaryPath = $"{primaryViewStatePath}.{Environment.ProcessId}.tmp";
                File.WriteAllText(temporaryPath, JsonSerializer.Serialize(state));
                File.Move(temporaryPath, primaryViewStatePath, overwrite: true);
                LoggerService.Log(
                    $"ViewWindowManager: Published primary view state enabled={enabled}, view={view}.");
            }
            catch (Exception exception)
            {
                LoggerService.LogException("ViewWindowManager.PublishPrimaryViewState", exception);
            }
        }

        private static void QueuePrimaryViewStateUpdate()
        {
            lock (primaryViewStateLock)
            {
                primaryViewStateDebounceTimer.Change(150, Timeout.Infinite);
            }
        }

        private static void ApplyPublishedPrimaryViewState()
        {
            PublishedPrimaryViewState? state;
            try
            {
                state = JsonSerializer.Deserialize<PublishedPrimaryViewState>(File.ReadAllText(primaryViewStatePath));
            }
            catch (Exception exception) when (exception is IOException or JsonException)
            {
                LoggerService.LogException("ViewWindowManager: Failed to read published primary view state.", exception);
                return;
            }

            if (state is null || state.ProcessId == Environment.ProcessId)
            {
                return;
            }

            Window? window = primaryViewWindow;
            if (window is null)
            {
                LoggerService.Log(
                    $"ViewWindowManager: Received primary view state enabled={state.Enabled}, view={state.View}, but this process has no local primary view.",
                    LoggerStatusEnum.WARNING);
                return;
            }

            bool enqueued = window.DispatcherQueue.TryEnqueue(() =>
            {
                if (!ReferenceEquals(primaryViewWindow, window))
                {
                    LoggerService.Log("ViewWindowManager: Skipped primary view state because the owning window changed.", LoggerStatusEnum.WARNING);
                    return;
                }

                try
                {
                    LoggerService.Log(
                        $"ViewWindowManager: Applying primary view state enabled={state.Enabled}, view={state.View} from process {state.ProcessId}.");
                    SettingsRetriever.SetPrimaryView(state.View);
                    SettingsRetriever.SetPrimaryViewEnabled(state.Enabled);

                    if (!state.Enabled)
                    {
                        ClosePrimaryView("primary view disabled by another process");
                    }
                    else if (primaryViewWindow is not null && primaryViewType != state.View)
                    {
                        ReplaceOwnedPrimaryView(state.View);
                    }
                    else
                    {
                        OpenPrimaryView(replaceExisting: true);
                    }
                }
                catch (Exception exception)
                {
                    LoggerService.LogException("ViewWindowManager: Failed applying primary view state.", exception);
                }
            });

            LoggerService.Log(
                $"ViewWindowManager: Primary view state update enqueued={enqueued} from process {state.ProcessId}.",
                enqueued ? LoggerStatusEnum.INFO : LoggerStatusEnum.WARNING);
        }

        private static void OpenPinnedView()
        {
            if (pinnedViewWindow is null)
            {
                if (Win32Interop.TryActivateWindowByTitle("GCaLink Pinned View"))
                {
                    return;
                }

                pinnedViewWindow = new PinnedViewWindow();
                pinnedViewWindow.Closed += PinnedViewClosed;
            }

            pinnedViewWindow.Activate();
        }

        private static bool TryActivateExistingPrimaryView()
        {
            return SetExistingPrimaryViewMode(primaryViewManagerActive);
        }

        private static bool SetExistingPrimaryViewMode(bool widgetManagerActive)
        {
            return Win32Interop.TrySetWidgetManagerActiveByTitle("GCaLink Day View", widgetManagerActive) ||
                Win32Interop.TrySetWidgetManagerActiveByTitle("GCaLink Week View", widgetManagerActive) ||
                Win32Interop.TrySetWidgetManagerActiveByTitle("GCaLink Todo View", widgetManagerActive);
        }

        private static void ClosePinnedView()
        {
            PinnedViewWindow? window = pinnedViewWindow;
            pinnedViewWindow = null;
            window?.Close();
        }

        private static void PrimaryViewClosed(object sender, WindowEventArgs args)
        {
            LoggerService.Log(
                $"ViewWindowManager: Primary calendar view Closed event received (process={Environment.ProcessId}).",
                LoggerStatusEnum.WARNING);
            if (ReferenceEquals(primaryViewWindow, sender))
            {
                primaryViewWindow = null;
                primaryViewType = null;
                PrimaryViewClosedByUser?.Invoke();
            }
        }

        private static void PinnedViewClosed(object sender, WindowEventArgs args)
        {
            if (ReferenceEquals(pinnedViewWindow, sender))
            {
                pinnedViewWindow = null;
            }
        }
    }
}