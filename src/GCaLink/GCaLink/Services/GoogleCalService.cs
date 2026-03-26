using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System.Threading;

using GCaLink.Models;
using Google.Apis.Calendar.v3.Data;
using MessagePack;

namespace GCaLink.Services
{
    public sealed class GoogleCalService
    {
        private readonly GoogleCalOptions _options;
        private static readonly string[] Scopes = { CalendarService.Scope.CalendarReadonly };

        public GoogleCalService(GoogleCalOptions options)
        {
            _options = options;
        }


        public async Task<CalendarService> CreateCalendarServiceAsync()
        {
            ClientSecrets secrets = new ClientSecrets
            {
                ClientId = _options.ClientId,
                ClientSecret = _options.ClientSecret
            };

            UserCredential credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                secrets,
                Scopes,
                "user",
                CancellationToken.None,
                new FileDataStore(Path.GetDirectoryName(_options.TokenPath) ?? "token-store", true));

            return new CalendarService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "GCWidget"
            });
        }

        public async Task<Dictionary<string, CalEventDto>> FetchUpcomingEventsAsync(CalendarService service)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            DateTimeOffset end = now.AddDays(7);

            ColorsResource.GetRequest colorsRequest = service.Colors.Get();
            Colors colors = await colorsRequest.ExecuteAsync();

            EventsResource.ListRequest eventsRequest = service.Events.List("primary");
            eventsRequest.TimeMinDateTimeOffset = now;
            eventsRequest.TimeMaxDateTimeOffset = end;
            eventsRequest.MaxResults = 15;
            eventsRequest.SingleEvents = true;
            eventsRequest.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;

            Events events = await eventsRequest.ExecuteAsync();

            Dictionary<string, CalEventDto> result = new Dictionary<string, CalEventDto>();

            if (events.Items == null)
                return result;

            foreach (var ev in events.Items)
            {
                var normalized = NormalizeEvent(ev, colors, _options.DefaultColor);
                result[ev.Id] = normalized;
            }

            return result;
        }

        private static CalEventDto NormalizeEvent(
            Google.Apis.Calendar.v3.Data.Event ev,
            Google.Apis.Calendar.v3.Data.Colors colors,
            string defaultColor)
        {
            DateTimeOffset start = ParseEventStart(ev);
            string color = GetEventColor(ev, colors, defaultColor);
            string source = GetEventSource(ev);

            return new CalEventDto
            {
                Title = ev.Summary ?? "",
                Datetime = start.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss"),
                Link = ev.HtmlLink ?? "",
                CustomConfig = false,
                Image = "",
                Color = color,
                Source = source
            };
        }

        private static DateTimeOffset ParseEventStart(Google.Apis.Calendar.v3.Data.Event ev)
        {
            if (ev.Start?.DateTimeDateTimeOffset != null)
                return ev.Start.DateTimeDateTimeOffset.Value;

            if (ev.Start?.Date != null && DateTime.TryParse(ev.Start.Date, out var allDay))
                return allDay;

            return DateTime.MinValue;
        }

        private static string GetEventColor(
            Google.Apis.Calendar.v3.Data.Event ev,
            Google.Apis.Calendar.v3.Data.Colors colors,
            string defaultColor)
        {
            if (!string.IsNullOrWhiteSpace(ev.ColorId) &&
                colors.Event__ != null &&
                colors.Event__.ContainsKey(ev.ColorId) &&
                !string.IsNullOrWhiteSpace(colors.Event__[ev.ColorId].Background))
            {
                return colors.Event__[ev.ColorId].Background;
            }

            return defaultColor;
        }

        private static string GetEventSource(Google.Apis.Calendar.v3.Data.Event ev)
        {
            Event.OrganizerData organizer = ev.Organizer;

            if (organizer == null)
                return "Google Calendar";

            if (!string.IsNullOrWhiteSpace(organizer.DisplayName))
                return organizer.DisplayName;

            if (organizer.Self == true)
                return "Me";

            if (!string.IsNullOrWhiteSpace(organizer.Email))
                return organizer.Email;

            return "Google Calendar";
        }
    }
}