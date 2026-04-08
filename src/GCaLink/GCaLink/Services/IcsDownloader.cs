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

        public async Task<string> DownloadIcsAsync(string icsUrl, string? filePath = null, CancellationToken cancellationToken = default)
        {
            try
            {
                byte[] content = await _client.GetByteArrayAsync(icsUrl, cancellationToken);

                if (!string.IsNullOrEmpty(filePath))
                {
                    await File.WriteAllBytesAsync(filePath, content, cancellationToken);
                    return filePath;
                }

                return Encoding.UTF8.GetString(content);
            }
            catch (HttpRequestException ex)
            {
                LoggerService.LogWarning($"Failed to download ICS from {icsUrl}: {ex.Message}", true);
                throw new InvalidOperationException($"Failed to download ICS from {icsUrl}: {ex.Message}", ex);
            }
        }
    }
}
