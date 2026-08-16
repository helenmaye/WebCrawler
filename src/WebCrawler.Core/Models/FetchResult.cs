using System.Net;

namespace WebCrawler.Core.Models;

public record FetchResult()
{
    public string? Html { get; init; }
    public HttpStatusCode? StatusCode { get; init; }
    public bool Success { get; init; }
    public string? Error { get; init; }
};