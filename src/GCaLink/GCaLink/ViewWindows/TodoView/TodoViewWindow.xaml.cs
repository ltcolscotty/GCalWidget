using GCaLink.ViewWindows;
using Microsoft.UI.Xaml;

namespace GCaLink.ViewWindows.TodoView
{
    public sealed partial class TodoViewWindow : Window
    {
        public TodoViewWindow()
        {
            InitializeComponent();
            WindowConfiguration.Configure(this, isWidgetViewActive: false, ManagedWindowKind.Primary);
            AppWindow.Resize(new Windows.Graphics.SizeInt32(360, 500));
        }
    }
}
