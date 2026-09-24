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
            ManagedWindowKind? managedWindowKind = null,
            Windows.Graphics.SizeInt32? defaultSize = null)
        {
            if (window.AppWindow.Presenter is OverlappedPresenter presenter)
            {
                presenter.IsResizable = true;
                if (!isWidgetViewActive)
                {
                    presenter.SetBorderAndTitleBar(hasBorder: true, hasTitleBar: false);
                    presenter.IsMinimizable = false;
                    presenter.IsMaximizable = false;
                }
            }

            ApplyBackground(window);

            if (managedWindowKind is not ManagedWindowKind kind)
            {
                return;
            }

            bool pinned = kind == ManagedWindowKind.Pinned;
            WindowSize? savedSize = SettingsRetriever.GetWindowSize(pinned);
            Windows.Graphics.SizeInt32 size = savedSize is not null
                ? new Windows.Graphics.SizeInt32(savedSize.Width, savedSize.Height)
                : defaultSize ?? new Windows.Graphics.SizeInt32(360, 500);
            window.AppWindow.Resize(size);

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
                if (args.DidPositionChange || args.DidSizeChange)
                {
                    SaveWindowState(window, pinned);
                }
            };
            window.Closed += (_, _) => SaveWindowState(window, pinned);
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

        private static void SaveWindowState(Window window, bool pinned)
        {
            Windows.Graphics.PointInt32 position = window.AppWindow.Position;
            Windows.Graphics.SizeInt32 size = window.AppWindow.Size;
            SettingsRetriever.SetWindowPosition(pinned, position.X, position.Y);
            SettingsRetriever.SetWindowSize(pinned, size.Width, size.Height);
        }
    }
}
