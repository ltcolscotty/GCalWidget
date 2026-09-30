using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using GCaLink.Models;
using GCaLink.Services;
using GCaLink.ViewWindows;
using Microsoft.UI.Xaml;

namespace GCaLink.ViewWindows.WeekView
{
    public sealed partial class WeekViewWindow : Window
    {
        public ObservableCollection<WeekDayDisplay> Days { get; } = new();

        public WeekViewWindow()
        {
            InitializeComponent();
            WindowConfiguration.Configure(
                this,
                isWidgetViewActive: false,
                ManagedWindowKind.Primary,
                new Windows.Graphics.SizeInt32(640, 500));

            DaysList.ItemsSource = Days;
            Activated += WeekViewWindow_Activated;
            EventAggService.EventsChanged += OnEventsChanged;
            Closed += OnClosed;
        }

        private async void WeekViewWindow_Activated(object sender, WindowActivatedEventArgs e)
        {
            if (e.WindowActivationState != WindowActivationState.Deactivated)
            {
                await LoadUpcomingEventsAsync();
            }
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
            Days.Clear();
            DateTime today = DateTime.Today;
            for (int dayOffset = 0; dayOffset < 7; dayOffset++)
            {
                DateTime date = today.AddDays(dayOffset);
                Days.Add(new WeekDayDisplay
                {
                    Day = date.ToString("ddd"),
                    Date = date.ToString("MMM d")
                });
            }

            string dataPath = SettingsRetriever.GetMainDataPath();
            if (!File.Exists(dataPath))
            {
                return;
            }

            var storedEvents = await EventAggService.ReadUpcomingEventsMessagePackAsync(dataPath);
            foreach (CalEventDto calendarEvent in storedEvents.Values
                .Where(calendarEvent =>
                {
                    DateTime localDate = calendarEvent.Datetime.ToLocalTime().Date;
                    return localDate >= today && localDate < today.AddDays(7);
                })
                .OrderBy(calendarEvent => calendarEvent.Datetime))
            {
                DateTime localDate = calendarEvent.Datetime.ToLocalTime().Date;
                WeekDayDisplay day = Days[(localDate - today).Days];
                day.Events.Add(new CalEventDisplay
                {
                    Time = calendarEvent.Datetime.ToLocalTime().ToString("h:mm tt"),
                    Title = calendarEvent.Title
                });
            }
        }
    }

    public sealed class WeekDayDisplay : INotifyPropertyChanged
    {
        public string Day { get; set; } = "";
        public string Date { get; set; } = "";
        public ObservableCollection<CalEventDisplay> Events { get; } = new();
        public Visibility EmptyStateVisibility => Events.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

        public event PropertyChangedEventHandler? PropertyChanged;

        public WeekDayDisplay()
        {
            Events.CollectionChanged += (_, _) => OnPropertyChanged(nameof(EmptyStateVisibility));
        }

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
