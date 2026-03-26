using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GCaLink.Models
{
    public sealed class GoogleCalOptions
    {
        public string ClientId { get; init; } = "";
        public string ClientSecret { get; init; } = "";
        public string TokenPath { get; init; } = "";
        public string DefaultColor { get; init; } = "#ff00ff";
    }
}
