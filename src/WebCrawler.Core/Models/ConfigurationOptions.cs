namespace WebCrawler.Core.Models;

public record ConfigurationOptions()
{
    public int MaxConcurrency { get; init; } = 10;
    public int MaxPages { get; init; } = 500;
}