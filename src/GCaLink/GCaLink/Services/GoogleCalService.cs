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
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace GCaLink.Services
{
    public sealed record GoogleCalendarSource(string AccountEmail, string CalendarId, string DisplayName);

    public sealed class GoogleCalService
    {
        public const string ProviderName = "Google";
        public const string PrimaryCalendarId = "primary";
        private const int ApiPageSize = 100;

        private readonly GoogleCalOptions _options;
        private static readonly string[] Scopes =
        {
            CalendarService.Scope.CalendarReadonly,
            "openid",
            "email",
            "profile"
        };

        public GoogleCalService(GoogleCalOptions options)
        {
            _options = options;
        }

        private static ClientSecrets LoadClientSecrets()
        {
            string? configuredPath = Environment.GetEnvironmentVariable("GOOGLE_CREDENTIALS_DEV_PATH");
            string credentialsPath = string.IsNullOrWhiteSpace(configuredPath)
                ? Path.Combine(AppContext.BaseDirectory, "credentials.json")
                : configuredPath;

            if (!File.Exists(credentialsPath))
            {
                throw new FileNotFoundException(
                    "Google OAuth credentials were not found. Set GOOGLE_CREDENTIALS_DEV_PATH or include credentials.json with the app.",
                    credentialsPath);
            }

            return GoogleClientSecrets.FromFile(credentialsPath).Secrets;
        }

        private GoogleAuthorizationCodeFlow CreateFlow()
        {
            ClientSecrets secrets = LoadClientSecrets();

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

        public async Task<bool> AuthorizeAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                using GoogleAuthorizationCodeFlow flow = CreateFlow();
                TokenResponse? token = await flow.LoadTokenAsync("user", cancellationToken);
                if (token == null)
                {
                    UserCredential credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                        flow.ClientSecrets,
                        Scopes,
                        "user",
                        cancellationToken,
                        flow.DataStore);
                    return credential.Token != null &&
                        (!credential.Token.IsStale || await credential.RefreshTokenAsync(cancellationToken));
                }

                UserCredential existingCredential = new UserCredential(flow, "user", token);
                return !existingCredential.Token.IsStale ||
                    await existingCredential.RefreshTokenAsync(cancellationToken);
            }
            catch
            {
                return false;
            }
        }

        public async Task<GoogleAccountProfile?> GetAccountProfileAsync(CancellationToken cancellationToken = default)
        {
            const string userId = "user";

            try
            {
                using GoogleAuthorizationCodeFlow flow = CreateFlow();
                TokenResponse? token = await flow.LoadTokenAsync(userId, cancellationToken);
                if (token == null)
                    return null;

                UserCredential credential = new UserCredential(flow, userId, token);
                if (credential.Token.IsStale && !await credential.RefreshTokenAsync(cancellationToken))
                    return null;

                using HttpClient client = new();
                using HttpRequestMessage request = new(
                    HttpMethod.Get,
                    "https://openidconnect.googleapis.com/v1/userinfo");
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
                    "Bearer",
                    credential.Token.AccessToken);

                using HttpResponseMessage response = await client.SendAsync(request, cancellationToken);
                if (!response.IsSuccessStatusCode)
                    return null;

                using JsonDocument document = JsonDocument.Parse(
                    await response.Content.ReadAsStringAsync(cancellationToken));
                JsonElement profile = document.RootElement;
                string name = profile.TryGetProperty("name", out JsonElement nameElement)
                    ? nameElement.GetString() ?? string.Empty
                    : string.Empty;
                string email = profile.TryGetProperty("email", out JsonElement emailElement)
                    ? emailElement.GetString() ?? string.Empty
                    : string.Empty;
                string picture = profile.TryGetProperty("picture", out JsonElement pictureElement)
                    ? pictureElement.GetString() ?? string.Empty
                    : string.Empty;

                return new GoogleAccountProfile(name, email, picture);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch
            {
                return null;
            }
        }

        public async Task SignOutAsync()
        {
            using GoogleAuthorizationCodeFlow flow = CreateFlow();
            await flow.DataStore.ClearAsync();
        }

        public async Task<CalendarService> CreateCalendarServiceAsync(CancellationToken cancellationToken = default)
        {
            ClientSecrets secrets = LoadClientSecrets();

            UserCredential credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                secrets,
                Scopes,
                "user",
                cancellationToken,
                new FileDataStore(Path.GetDirectoryName(_options.TokenPath) ?? "token-store", true)
            );

            return new CalendarService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "GCWidget"
            });
        }

        public async Task<IReadOnlyList<GoogleCalendarSource>> GetCalendarSourcesAsync()
        {
            GoogleAccountProfile? profile = await GetAccountProfileAsync();
            if (profile == null || string.IsNullOrWhiteSpace(profile.Email))
            {
                return Array.Empty<GoogleCalendarSource>();
            }

            CalendarService service = await CreateCalendarServiceAsync();
            List<GoogleCalendarSource> sources = new();
            string? pageToken = null;
            do
            {
                CalendarListResource.ListRequest request = service.CalendarList.List();
                request.MaxResults = ApiPageSize;
                request.PageToken = pageToken;
                CalendarList calendars = await request.ExecuteAsync();
                if (calendars.Items != null)
                {
                    foreach (CalendarListEntry calendar in calendars.Items)
                    {
                        if (string.IsNullOrWhiteSpace(calendar.Id))
                        {
                            continue;
                        }

                        sources.Add(new GoogleCalendarSource(
                            profile.Email,
                            calendar.Primary == true ? PrimaryCalendarId : calendar.Id,
                            string.IsNullOrWhiteSpace(calendar.Summary) ? calendar.Id : calendar.Summary));
                    }
                }

                pageToken = calendars.NextPageToken;
            }
            while (!string.IsNullOrWhiteSpace(pageToken));

            return sources;
        }

        public async Task<(Dictionary<IDHelper.EventID, CalEventDto> Events, List<IDHelper.EventID> EventIds, bool IsComplete)> FetchUpcomingEventsAsync(
            CalendarService service, 
            Dictionary<IDHelper.EventID, CalEventDto> calendarData,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            DateTimeOffset now = DateTimeOffset.UtcNow;
            DateTimeOffset end = now.AddDays(7);
            List<IDHelper.EventID> sourceKeys = new();
            int maximumEvents = Math.Max(1, _options.MaximumEventsPerRefresh);
            int fetchedCount = 0;
            string accountEmail = (await GetAccountProfileAsync(cancellationToken))?.Email ?? string.Empty;

            ColorsResource.GetRequest colorsRequest = service.Colors.Get();
            Colors colors = await colorsRequest.ExecuteAsync(cancellationToken);

            string? pageToken = null;
            do
            {
                EventsResource.ListRequest eventsRequest = service.Events.List(PrimaryCalendarId);
                eventsRequest.TimeMinDateTimeOffset = now;
                eventsRequest.TimeMaxDateTimeOffset = end;
                eventsRequest.MaxResults = Math.Min(ApiPageSize, maximumEvents - fetchedCount);
                eventsRequest.SingleEvents = true;
                eventsRequest.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;
                eventsRequest.PageToken = pageToken;

                Events events = await eventsRequest.ExecuteAsync(cancellationToken);
                if (events.Items != null)
                {
                    foreach (Event ev in events.Items)
                    {
                        if (string.IsNullOrWhiteSpace(ev.Id))
                        {
                            throw new InvalidDataException("Google Calendar returned an event without an ID.");
                        }

                        IDHelper.EventID id = IDHelper.GetEventID(ev.Id, PrimaryCalendarId, ProviderName);
                        sourceKeys.Add(id);
                        calendarData[id] = NormalizeEvent(ev, colors, _options.DefaultColor, id, accountEmail);
                        fetchedCount++;
                    }
                }

                pageToken = events.NextPageToken;
                if (fetchedCount >= maximumEvents)
                {
                    LoggerService.Log(
                        $"Google Calendar refresh reached the configured limit of {maximumEvents} events; results may be truncated.",
                        LoggerStatusEnum.WARNING);
                    if (!string.IsNullOrEmpty(pageToken))
                    {
                        return (calendarData, sourceKeys, false);
                    }
                }
            }
            while (!string.IsNullOrEmpty(pageToken));

            return (calendarData, sourceKeys, true);
        }

        private static CalEventDto NormalizeEvent(Event ev, Colors colors, string defaultColor, IDHelper.EventID evId, string accountEmail)
        {
            DateTimeOffset start = ParseEventStart(ev);
            string color = GetEventColor(ev, colors, defaultColor);
            string source = GetEventSource(ev);

            return new CalEventDto
            {
                Id = evId,
                Title = ev.Summary ?? "",
                Datetime = start.ToUniversalTime(),
                Link = ev.HtmlLink ?? "",
                CustomConfig = false,
                Image = "",
                Color = color,
                Source = source,
                Provider = ProviderName,
                CalendarId = PrimaryCalendarId,
                GoogleAccountEmail = accountEmail
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