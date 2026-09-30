using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

using Microsoft.UI.Xaml;

using GCaLink.Models;
using GCaLink.Services;
using GCaLink.ViewWindows;
using System.Threading.Tasks;

namespace GCaLink.ViewWindows.DayView
{
public sealed partial class DayViewWindow : Window
{
    public ObservableCollection<CalEventDisplay> Events { get; } = new();
    private DateTime _loadedDate = DateTime.MinValue;

    public DayViewWindow()
    {
        InitializeComponent();
        WindowConfiguration.Configure(
            this,
            isWidgetViewActive: false,
            ManagedWindowKind.Primary,
            new Windows.Graphics.SizeInt32(300, 500));

        Activated += DayViewWindow_Activated;
        EventAggService.EventsChanged += OnEventsChanged;
        SourceImageService.Instance.SourceImagesChanged += OnSourceImagesChanged;
        Closed += OnClosed;
        ScheduleList.ItemsSource = Events;
    }

    private async void DayViewWindow_Activated(object sender, WindowActivatedEventArgs e)
    {
        if (e.WindowActivationState == WindowActivationState.Deactivated || _loadedDate == DateTime.Today)
        {
            return;
        }

        await LoadTodayEventsAsync();
    }

    private void OnEventsChanged(object? sender, EventArgs e)
    {
        DispatcherQueue.TryEnqueue(() => _ = LoadTodayEventsAsync());
    }

    private void OnSourceImagesChanged(object? sender, EventArgs e)
    {
        DispatcherQueue.TryEnqueue(() => _ = LoadTodayEventsAsync());
    }

    private void OnClosed(object sender, WindowEventArgs e)
    {
        EventAggService.EventsChanged -= OnEventsChanged;
        SourceImageService.Instance.SourceImagesChanged -= OnSourceImagesChanged;
    }

    private async Task LoadTodayEventsAsync()
    {
        _loadedDate = DateTime.Today;
        Events.Clear();

        DateTime today = DateTime.Today;
        DayText.Text = today.ToString("dddd");
        DateText.Text = today.ToString("MMMM d");

        string dataPath = SettingsRetriever.GetMainDataPath();
        if (!File.Exists(dataPath))
        {
            EmptyState.Visibility = Visibility.Visible;
            return;
        }

        var storedEvents = await EventAggService.ReadUpcomingEventsMessagePackAsync(dataPath);
        foreach (CalEventDto calendarEvent in storedEvents.Values
            .Where(calendarEvent => calendarEvent.Datetime.ToLocalTime().Date == today)
            .OrderBy(calendarEvent => calendarEvent.Datetime))
        {
            Events.Add(CalendarEventDisplayService.Create(
                calendarEvent,
                calendarEvent.Datetime.ToLocalTime().ToString("h:mm tt")));
        }

        EmptyState.Visibility = Events.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }
}
}

