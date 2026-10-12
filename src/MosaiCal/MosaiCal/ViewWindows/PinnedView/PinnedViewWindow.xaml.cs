using Microsoft.UI.Xaml;
using MosaiCal.Services;

namespace MosaiCal.ViewWindows.PinnedView
{
    public sealed partial class PinnedViewWindow : Window
    {
        public PinnedViewWindow()
        {
            InitializeComponent();
            WindowConfiguration.Configure(
                this,
                isWidgetViewActive: false,
                ManagedWindowKind.Pinned,
                new Windows.Graphics.SizeInt32(360, 500));
            PinnedListPane.Visibility = SettingsRetriever.GetPinnedViewEnabled()
                ? Visibility.Visible
                : Visibility.Collapsed;
        }
    }
}
