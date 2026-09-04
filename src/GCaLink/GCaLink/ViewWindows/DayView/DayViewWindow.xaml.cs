using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.UI.Xaml;

using GCaLink.Models;

namespace GCaLink.ViewWindows.DayView
{
public sealed partial class DayViewWindow : Window
{
    public ObservableCollection<CalEventDisplay> Events { get; } = new();

    public DayViewWindow()
    {
        InitializeComponent();

        int width = 300;
        int height = 500;

        this.AppWindow.Resize(new Windows.Graphics.SizeInt32(width, height));

        var today = DateTime.Now;

        DayText.Text = today.ToString("dddd");
        DateText.Text = today.ToString("MMMM d");

        Events.Add(new CalEventDisplay
        {
            Time = "9:00",
            Title = "Daily Standup",
            Location = "Teams"
        });

        Events.Add(new CalEventDisplay
        {
            Time = "11:30",
            Title = "Design Review",
            Location = "Conference Room A"
        });

        Events.Add(new CalEventDisplay
        {
            Time = "2:00",
            Title = "Project Planning",
            Location = "Teams"
        });

        Events.Add(new CalEventDisplay
        {
            Time = "4:30",
            Title = "Code Review",
            Location = "GitHub"
        });

        ScheduleList.ItemsSource = Events;
    }
}
}

