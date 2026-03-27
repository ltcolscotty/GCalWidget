using MessagePack;

namespace GCaLink.Models
{
    [MessagePackObject]
    public class CalEventDto
    {
        [Key(0)] public string Title { get; set; } = "";
        [Key(1)] public string Datetime { get; set; } = "";
        [Key(2)] public string Link { get; set; } = "";
        [Key(3)] public bool CustomConfig { get; set; } = false;
        [Key(4)] public string Image { get; set; } = "";
        [Key(5)] public string Color { get; set; } = "";
        [Key(6)] public string Source { get; set; } = "";
    }
}