using GCaLink.Services;
using MessagePack;
using System;

namespace GCaLink.Models
{
    [MessagePackObject]
    public class CalEventDto
    {
        [Key(0)] public IDHelper.EventID Id { get; set; } = IDHelper.GetEventID();
        [Key(1)] public string Title { get; set; } = "";
        [Key(2)] public DateTimeOffset Datetime { get; set; } = DateTimeOffset.Now;
        [Key(3)] public string Link { get; set; } = "";
        [Key(4)] public bool CustomConfig { get; set; } = false;
        [Key(5)] public string Image { get; set; } = "";
        [Key(6)] public string Color { get; set; } = "";
        [Key(7)] public string Source { get; set; } = "";
        [Key(8)] public string LongSource { get; set; } = "";
        [Key(9)] public string Provider { get; set; } = "";
        [Key(10)] public string CalendarId { get; set; } = "";
        [Key(11)] public string GoogleAccountEmail { get; set; } = "";
    }
}