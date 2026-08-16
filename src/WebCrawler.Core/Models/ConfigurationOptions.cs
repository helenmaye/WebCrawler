namespace WebCrawler.Core.Models;

public record ConfigurationOptions()
{
    public int MaxConcurrency { get; init; } = 10;
}