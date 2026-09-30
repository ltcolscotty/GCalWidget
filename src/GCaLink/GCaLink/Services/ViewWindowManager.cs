using System;
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
        private static Window? primaryViewWindow;
        private static PrimaryViewEnum? primaryViewType;
        private static PinnedViewWindow? pinnedViewWindow;
        private static bool primaryViewDraggable;

        public static event Action? PrimaryViewClosedByUser;

        public static void SetPrimaryViewDraggable(bool draggable)
        {
            primaryViewDraggable = draggable;
            if (primaryViewWindow is not null)
            {
                WindowConfiguration.SetWindowDraggable(primaryViewWindow, draggable);
            }
        }

        public static void SyncWithSettings(bool replacePrimaryView = false)
        {
            if (SettingsRetriever.GetPrimaryViewEnabled())
            {
                OpenPrimaryView(replacePrimaryView);
            }
            else
            {
                ClosePrimaryView();
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
            if (primaryViewWindow is not null &&
                (!replaceExisting || primaryViewType == selectedView))
            {
                primaryViewWindow.Activate();
                return;
            }

            ClosePrimaryView();
            if (TryActivateExistingPrimaryView())
            {
                return;
            }

            primaryViewWindow = selectedView switch
            {
                PrimaryViewEnum.Day => new DayViewWindow(),
                PrimaryViewEnum.Week => new WeekViewWindow(),
                _ => new TodoViewWindow()
            };
            WindowConfiguration.SetWindowDraggable(primaryViewWindow, primaryViewDraggable);
            primaryViewType = selectedView;
            primaryViewWindow.Closed += PrimaryViewClosed;
            primaryViewWindow.Activate();
        }

        private static void ClosePrimaryView()
        {
            Window? window = primaryViewWindow;
            primaryViewWindow = null;
            primaryViewType = null;
            window?.Close();
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
            return Win32Interop.TryActivateWindowByTitle("GCaLink Day View") ||
                Win32Interop.TryActivateWindowByTitle("GCaLink Week View") ||
                Win32Interop.TryActivateWindowByTitle("GCaLink Todo View");
        }

        private static void ClosePinnedView()
        {
            PinnedViewWindow? window = pinnedViewWindow;
            pinnedViewWindow = null;
            window?.Close();
        }

        private static void PrimaryViewClosed(object sender, WindowEventArgs args)
        {
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