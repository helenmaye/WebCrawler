using System.Net;

namespace WebCrawler.Core.Models;

public record CrawlResult(
    IReadOnlyList<string>? Links,
    HttpStatusCode? StatusCode,
    string? Error)
{
    public virtual bool Equals(CrawlResult? other) =>
        other is not null &&
        StatusCode == other.StatusCode &&
        Error == other.Error &&
        (Links ?? Enumerable.Empty<string>()).SequenceEqual(other.Links ?? Enumerable.Empty<string>());

    public override int GetHashCode() => HashCode.Combine(Links?.Count ?? 0, StatusCode, Error);
}