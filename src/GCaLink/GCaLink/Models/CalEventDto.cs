using MessagePack;
using System;

namespace GCaLink.Models
{
    [MessagePackObject]
    public class CalEventDto
    {
        [Key(0)] public string Id { get; set; } = "";
        [Key(1)] public string Title { get; set; } = "";
        [Key(2)] public DateTimeOffset Datetime { get; set; } = DateTimeOffset.Now;
        [Key(3)] public string Link { get; set; } = "";
        [Key(4)] public bool CustomConfig { get; set; } = false;
        [Key(5)] public string Image { get; set; } = "";
        [Key(6)] public string Color { get; set; } = "";
        [Key(7)] public string Source { get; set; } = "";
        [Key(8)] public string LongSource { get; set; } = "";
    }
}