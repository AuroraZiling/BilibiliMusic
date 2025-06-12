using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using BiliBiliMusic.Extensions;

namespace BiliBiliMusic.Helpers;

public static partial class BiliBiliHelper
{
    private static HttpClient HttpClient { get; set; } = new(
        new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.GZip,
            ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
        })
    {
        Timeout = TimeSpan.FromSeconds(10),
        DefaultRequestHeaders =
        {
            UserAgent = { 
                new ProductInfoHeaderValue("Mozilla", "5.0"),
                new ProductInfoHeaderValue("(Windows NT 10.0; Win64; x64)"),
                new ProductInfoHeaderValue("AppleWebKit", "537.36"),
                new ProductInfoHeaderValue("(KHTML, like Gecko)"),
                new ProductInfoHeaderValue("Chrome", "58.0.3029.110"),
                new ProductInfoHeaderValue("Safari", "537.3"),
                new ProductInfoHeaderValue("BiliBiliMusic", "1.0")
            },
            Connection = { "keep-alive" },
            Accept =
            {
                new MediaTypeWithQualityHeaderValue("*/*")
            },
            AcceptEncoding =
            {
                new StringWithQualityHeaderValue("gzip"), 
                new StringWithQualityHeaderValue("deflate"),
                new StringWithQualityHeaderValue("br")
            }
        }
    };

    public static string ExtractVideoId(string url)
    {
        var match = ExtractVideoIdRegex().Match(url);
        return match.Success ? match.Groups[1].Value : string.Empty;
    }

    public static async Task<string> ResolveUrl(string url)
    {
        var response = await HttpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }

    public static async Task<bool> TestConnection(string url)
    {
        var response = await HttpClient.GetAsync(url);
        return response.IsSuccessStatusCode;
    }
    
    public static string? ExtractVideoTitle(string content)
    {
        var match = ExtractVideoTitleRegex().Match(content);
        return !match.Success ? null : match.Groups[1].Value.Replace("_哔哩哔哩_bilibili", "").GetValidFileName();
    }

    public static string? ExtractAudioUrl(string content)
    {
        var match = ExtractPlayInfoJsonRegex().Match(content);
        if (!match.Success) throw new Exception("PlayInfo JSON not found.");
        var json = match.Groups[1].Value;
        var jsonDocument = JsonDocument.Parse(json);
        return jsonDocument.RootElement
            .GetProperty("data")
            .GetProperty("dash")
            .GetProperty("audio")
            .EnumerateArray()
            .FirstOrDefault()
            .GetProperty("baseUrl")
            .GetString();
    }
    
    public static async Task DownloadAudioAsync(string audioUrl, string filePath, IProgress<HttpClientExtensions.HttpDownloadProgress> progress, CancellationToken cancellationToken)
    {
        var response = await HttpClient.GetByteArrayAsync(audioUrl, progress, cancellationToken);
        await using var fileStream = File.Create(filePath);
        await fileStream.WriteAsync(response, cancellationToken);
    }

    [GeneratedRegex(@"bilibili\.com/video/(BV[a-zA-Z0-9]+|av\d+)")]
    private static partial Regex ExtractVideoIdRegex();
    [GeneratedRegex(@"<script>window\.__playinfo__=(.*?)</script>", RegexOptions.Singleline)]
    private static partial Regex ExtractPlayInfoJsonRegex();
    [GeneratedRegex("<title[^>]*>(.*?)</title>")]
    private static partial Regex ExtractVideoTitleRegex();
}