using GCaLink.Platform;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using WinRT.Interop;

namespace GCaLink.ViewWindows
{
    internal static class DesktopWidgetWindowBehavior
    {
        public static void SetWidgetManagerActive(Window window, bool isActive)
        {
            if (window.AppWindow.Presenter is OverlappedPresenter presenter)
            {
                presenter.SetBorderAndTitleBar(hasBorder: true, hasTitleBar: isActive);
                presenter.IsResizable = isActive;
                presenter.IsMinimizable = false;
                presenter.IsMaximizable = false;
            }

            Win32Interop.SetDesktopWidgetMode(WindowNative.GetWindowHandle(window), isActive);
        }
    }
}