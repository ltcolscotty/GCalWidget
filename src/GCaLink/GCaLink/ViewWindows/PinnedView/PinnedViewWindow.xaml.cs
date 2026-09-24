using GCaLink.Services;
using GCaLink.ViewWindows;
using Microsoft.UI.Xaml;

namespace GCaLink.ViewWindows.PinnedView
{
    public sealed partial class PinnedViewWindow : Window
    {
        public PinnedViewWindow()
        {
            InitializeComponent();
            WindowConfiguration.Configure(this, isWidgetViewActive: false);
            AppWindow.Resize(new Windows.Graphics.SizeInt32(360, 500));
            PinnedListPane.Visibility = SettingsRetriever.GetPinnedViewEnabled()
                ? Visibility.Visible
                : Visibility.Collapsed;
        }
    }
}
