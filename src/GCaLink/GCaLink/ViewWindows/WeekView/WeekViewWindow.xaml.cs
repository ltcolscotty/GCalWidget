using GCaLink.ViewWindows;
using Microsoft.UI.Xaml;

namespace GCaLink.ViewWindows.WeekView
{
    public sealed partial class WeekViewWindow : Window
    {
        public WeekViewWindow()
        {
            InitializeComponent();
            WindowConfiguration.Configure(this, isWidgetViewActive: false);
            AppWindow.Resize(new Windows.Graphics.SizeInt32(640, 500));
        }
    }
}
