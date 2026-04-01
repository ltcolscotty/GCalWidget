using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using GCaLink.Models;

namespace GCaLink.Services
{
    internal class CanvasService
    {
        public CanvasService() { 
            IcsDownloader downloader = new IcsDownloader();
        }

        public async Task FetchUpcomingEventsAsync(string sourceLink)
        {

        }
    }
}
