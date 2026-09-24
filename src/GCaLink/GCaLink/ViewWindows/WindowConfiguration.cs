using GCaLink.Models;
using GCaLink.Services;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace GCaLink.ViewWindows
{
    internal enum ManagedWindowKind
    {
        Primary,
        Pinned
    }

    internal static class WindowConfiguration
    {
        public static void Configure(
            Window window,
            bool isWidgetViewActive,
            ManagedWindowKind? managedWindowKind = null)
        {
            if (window.AppWindow.Presenter is OverlappedPresenter presenter)
            {
                presenter.IsResizable = isWidgetViewActive;
            }

            ApplyBackground(window);

            if (managedWindowKind is not ManagedWindowKind kind)
            {
                return;
            }

            bool pinned = kind == ManagedWindowKind.Pinned;
            WindowPosition? savedPosition = SettingsRetriever.GetWindowPosition(pinned);
            if (savedPosition is not null)
            {
                window.AppWindow.Move(new Windows.Graphics.PointInt32
                {
                    X = savedPosition.X,
                    Y = savedPosition.Y
                });
            }

            window.AppWindow.Changed += (_, args) =>
            {
                if (args.DidPositionChange)
                {
                    SavePosition(window, pinned);
                }
            };
            window.Closed += (_, _) => SavePosition(window, pinned);
        }

        public static void ApplyBackground(Window window)
        {
            window.SystemBackdrop = SettingsRetriever.GetBackgroundType() switch
            {
                BackgroundTypeEnum.Mica => new MicaBackdrop(),
                BackgroundTypeEnum.Acrylic => new DesktopAcrylicBackdrop(),
                _ => null
            };
        }

        private static void SavePosition(Window window, bool pinned)
        {
            Windows.Graphics.PointInt32 position = window.AppWindow.Position;
            SettingsRetriever.SetWindowPosition(pinned, position.X, position.Y);
        }
    }
}
