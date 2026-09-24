using GCaLink.Models;
using GCaLink.ViewWindows.DayView;
using GCaLink.ViewWindows.PinnedView;
using GCaLink.ViewWindows.TodoView;
using GCaLink.ViewWindows.WeekView;
using Microsoft.UI.Xaml;

namespace GCaLink.Services
{
    internal static class ViewWindowManager
    {
        private static Window? primaryViewWindow;
        private static PrimaryViewEnum? primaryViewType;
        private static PinnedViewWindow? pinnedViewWindow;

        public static void SyncWithSettings()
        {
            if (SettingsRetriever.GetPrimaryViewEnabled())
            {
                OpenPrimaryView();
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

        public static void OpenPrimaryView()
        {
            PrimaryViewEnum selectedView = SettingsRetriever.GetPrimaryView();
            if (primaryViewWindow is not null && primaryViewType == selectedView)
            {
                primaryViewWindow.Activate();
                return;
            }

            ClosePrimaryView();
            primaryViewWindow = selectedView switch
            {
                PrimaryViewEnum.Day => new DayViewWindow(),
                PrimaryViewEnum.Week => new WeekViewWindow(),
                _ => new TodoViewWindow()
            };
            primaryViewType = selectedView;
            primaryViewWindow.Closed += PrimaryViewClosed;
            primaryViewWindow.Activate();
        }

        public static void ClosePrimaryView()
        {
            Window? window = primaryViewWindow;
            primaryViewWindow = null;
            primaryViewType = null;
            window?.Close();
        }

        public static void OpenPinnedView()
        {
            if (pinnedViewWindow is null)
            {
                pinnedViewWindow = new PinnedViewWindow();
                pinnedViewWindow.Closed += PinnedViewClosed;
            }

            pinnedViewWindow.Activate();
        }

        public static void ClosePinnedView()
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