using System.Net;

namespace WebCrawler.Core.Models;

public record FetchResult(Uri Uri)
{
    public Uri? Url { get; init; } = Uri;
    public string? Html { get; init; }
    public HttpStatusCode? StatusCode { get; init; }
    public bool Success { get; init; }
    public string? Error { get; init; }
};