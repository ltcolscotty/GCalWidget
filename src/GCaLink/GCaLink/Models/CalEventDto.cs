using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GCaLink.Models
{
    public sealed class CalEventDto
    {
        public string Title { get; init; } = "";
        public string Datetime { get; init; } = "";
        public string Link { get; init; } = "";
        public bool CustomConfig { get; init; } = false;
        public string Image { get; init; } = "";
        public string Color { get; init; } = "";
        public string Source { get; init; } = "";
    }
}
