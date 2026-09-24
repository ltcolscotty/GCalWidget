using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;

namespace GCaLink.ViewWindows
{
    internal static class WindowConfiguration
    {
        public static void Configure(Window window, bool isWidgetViewActive)
        {
            if (window.AppWindow.Presenter is OverlappedPresenter presenter)
            {
                presenter.IsResizable = isWidgetViewActive;
            }
        }
    }
}
