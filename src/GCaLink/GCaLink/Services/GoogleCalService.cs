using GCaLink.Models;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using Google.Apis.Util;
using Google.Apis.Util.Store;
using MessagePack;
using MessagePack.Formatters;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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

        private GoogleAuthorizationCodeFlow CreateFlow()
        {
            var secrets = new ClientSecrets
            {
                ClientId = _options.ClientId,
                ClientSecret = _options.ClientSecret
            };

            return new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = secrets,
                Scopes = Scopes,
                DataStore = new FileDataStore(
                    Path.GetDirectoryName(_options.TokenPath) ?? "token-store",
                    true),
                Clock = SystemClock.Default
            });
        }

        public async Task<bool> IsAccountActiveAsync()
        {
            const string userId = "user";

            try
            {
                using GoogleAuthorizationCodeFlow flow = CreateFlow();

                TokenResponse? token = await flow.LoadTokenAsync(userId, CancellationToken.None);
                if (token == null)
                    return false;

                UserCredential credential = new UserCredential(flow, userId, token);

                if (credential.Token == null)
                    return false;

                if (!credential.Token.IsStale)
                    return true;

                return await credential.RefreshTokenAsync(CancellationToken.None);
            }
            catch
            {
                return false;
            }
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
                new FileDataStore(Path.GetDirectoryName(_options.TokenPath) ?? "token-store", true)
            );

            return new CalendarService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "GCWidget"
            });
        }

        public async Task FetchUpcomingEventsAsync(CalendarService service, List<CalEventDto> calendarData)
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

            if (events.Items == null) return;

            foreach (Event ev in events.Items)
            {
                CalEventDto normalized = NormalizeEvent(ev, colors, _options.DefaultColor);
                calendarData.Add(normalized);
            }
        }

        private static CalEventDto NormalizeEvent(Event ev, Colors colors, string defaultColor)
        {
            DateTimeOffset start = ParseEventStart(ev);
            string color = GetEventColor(ev, colors, defaultColor);
            string source = GetEventSource(ev);

            return new CalEventDto
            {
                Id = ev.Id,
                Title = ev.Summary ?? "",
                Datetime = start.ToUniversalTime(),
                Link = ev.HtmlLink ?? "",
                CustomConfig = false,
                Image = "",
                Color = color,
                Source = source
            };
        }

        private static DateTimeOffset ParseEventStart(Event ev)
        {
            if (ev.Start?.DateTimeDateTimeOffset != null)
                return ev.Start.DateTimeDateTimeOffset.Value;

            if (ev.Start?.Date != null && DateTime.TryParse(ev.Start.Date, out var allDay))
                return allDay;

            return DateTime.MinValue;
        }

        private static string GetEventColor(Event ev, Colors colors, string defaultColor)
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

        private static string GetEventSource(Event ev)
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