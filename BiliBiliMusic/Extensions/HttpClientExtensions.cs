using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace BiliBiliMusic.Extensions;

public static class HttpClientExtensions
{
    private const int BufferSize = 8192;
    
    public class HttpDownloadProgress
    {
        public ulong BytesReceived { get; set; }
        public ulong TotalBytesToReceive { get; set; }
    }

    public static async Task<byte[]> GetByteArrayAsync(this HttpClient client, string url, IProgress<HttpDownloadProgress> progress, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(client);

        using var responseMessage = await client.GetAsync(new Uri(url), HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        responseMessage.EnsureSuccessStatusCode();

        var content = responseMessage.Content;

        var headers = content.Headers;
        var contentLength = headers.ContentLength;
        await using var responseStream = await content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        var buffer = new byte[BufferSize];
        int bytesRead;
        var bytes = new List<byte>();

        var downloadProgress = new HttpDownloadProgress();
        if (contentLength.HasValue)
        {
            downloadProgress.TotalBytesToReceive = (ulong)contentLength.Value;
        }
        progress.Report(downloadProgress);

        while ((bytesRead = await responseStream.ReadAsync(buffer.AsMemory(0, BufferSize), cancellationToken).ConfigureAwait(false)) > 0)
        {
            bytes.AddRange(buffer.Take(bytesRead));

            downloadProgress.BytesReceived += (ulong)bytesRead;
            progress?.Report(downloadProgress);
        }

        return bytes.ToArray();
    }
}