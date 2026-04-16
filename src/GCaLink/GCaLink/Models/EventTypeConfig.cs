using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MessagePack;

namespace GCaLink.Models
{
    [MessagePackObject]
    public class EventTypeConfig
    {
        [Key(0)] public string Source { get; set; } = "";
        [Key(1)] public int EventConfigVersion { get; } = 1;
        [Key(2)] public string BkgColor { get; set; } = "#ff00ff";
        [Key(3)] public string BkgImg { get; set; } = "";
        [Key(4)] public bool Enabled { get; set; } = true;

        public void Normallize()
        {
            //leave blank for now
        }
    }
}
