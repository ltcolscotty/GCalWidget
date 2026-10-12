using Microsoft.UI.Xaml.Media;

namespace MosaiCal.Models;

public class CalEventDisplay
{
    public string Time { get; set; } = "";
    public string Title { get; set; } = "";
    public string Location { get; set; } = "";
    public Brush BackgroundBrush { get; set; } = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 58, 58, 58));
}

