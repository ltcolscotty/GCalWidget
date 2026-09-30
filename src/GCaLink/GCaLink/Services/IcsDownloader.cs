using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GCaLink.Services
{
    internal class IcsDownloader
    {
        private HttpClient _client;

        public IcsDownloader() {
            _client = new HttpClient();
        }

        public async Task<string> DownloadIcsAsync(
            string icsUrl,
            string? filePath = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, icsUrl);

                // Some servers/CDNs reject requests without a User-Agent.
                request.Headers.UserAgent.ParseAdd("GCaLink/1.0");
                request.Headers.Accept.ParseAdd("text/calendar, text/plain, */*");

                using HttpResponseMessage response =
                    await _client.SendAsync(request, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    string responseBody =
                        await response.Content.ReadAsStringAsync(cancellationToken);

                    LoggerService.LogWarning(
                        $"Canvas ICS request failed. " +
                        $"HTTP {(int)response.StatusCode} ({response.StatusCode}). " +
                        $"Content-Type: {response.Content.Headers.ContentType}. " +
                        $"Response: {responseBody}",
                        LoggerStatusEnum.ERROR);
                }

                response.EnsureSuccessStatusCode();

                byte[] content =
                    await response.Content.ReadAsByteArrayAsync(cancellationToken);

                if (!string.IsNullOrEmpty(filePath))
                {
                    string? directory = Path.GetDirectoryName(filePath);

                    if (!string.IsNullOrEmpty(directory))
                        Directory.CreateDirectory(directory);

                    await File.WriteAllBytesAsync(
                        filePath,
                        content,
                        cancellationToken);

                    return filePath;
                }

                return Encoding.UTF8.GetString(content);
            }
            catch (HttpRequestException ex)
            {
                string statusCode =
                    ex.StatusCode?.ToString() ?? "unknown";

                LoggerService.LogWarning(
                    $"Failed to download Canvas ICS " +
                    $"(HTTP status: {statusCode}): {ex.Message}",
                    LoggerStatusEnum.ERROR);

                throw new InvalidOperationException(
                    $"Failed to download Canvas ICS " +
                    $"(HTTP status: {statusCode}): {ex.Message}",
                    ex);
            }
        }
    }
}
