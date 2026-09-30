using GCaLink.Models;
using GCaLink.Services;
using GCaLink.ViewWindows;
using Microsoft.UI.Xaml;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace GCaLink.ViewWindows.TodoView
{
    public sealed partial class TodoViewWindow : Window
    {
        public ObservableCollection<CalEventDisplay> Events { get; } = new();

        public TodoViewWindow()
        {
            InitializeComponent();
            WindowConfiguration.Configure(
                this,
                isWidgetViewActive: false,
                ManagedWindowKind.Primary,
                new Windows.Graphics.SizeInt32(360, 500));
            Activated += TodoViewWindow_Activated;
            EventAggService.EventsChanged += OnEventsChanged;
            Closed += OnClosed;
        }

        private async void TodoViewWindow_Activated(object sender, WindowActivatedEventArgs e)
        {
            if (e.WindowActivationState == WindowActivationState.Deactivated)
            {
                return;
            }

            await LoadUpcomingEventsAsync();
        }

        private void OnEventsChanged(object? sender, EventArgs e)
        {
            DispatcherQueue.TryEnqueue(() => _ = LoadUpcomingEventsAsync());
        }

        private void OnClosed(object sender, WindowEventArgs e)
        {
            EventAggService.EventsChanged -= OnEventsChanged;
        }

        private async Task LoadUpcomingEventsAsync()
        {
            Events.Clear();
            string dataPath = SettingsRetriever.GetMainDataPath();
            if (!File.Exists(dataPath))
            {
                EmptyState.Visibility = Visibility.Visible;
                return;
            }

            DateTimeOffset now = DateTimeOffset.Now;
            DateTimeOffset end = now.AddHours(24);
            var storedEvents = await EventAggService.ReadUpcomingEventsMessagePackAsync(dataPath);

            foreach (CalEventDto calendarEvent in storedEvents.Values
                .Where(calendarEvent => calendarEvent.Datetime >= now && calendarEvent.Datetime <= end)
                .OrderBy(calendarEvent => calendarEvent.Datetime))
            {
                Events.Add(new CalEventDisplay
                {
                    Time = calendarEvent.Datetime.ToLocalTime().ToString("ddd, h:mm tt"),
                    Title = calendarEvent.Title
                });
            }

            EmptyState.Visibility = Events.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
