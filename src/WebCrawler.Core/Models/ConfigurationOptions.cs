namespace WebCrawler.Core.Models;

public record ConfigurationOptions()
{
    public int ChannelCapacity { get; init; } = 100;
    public int MaxConcurrency { get; init; } = 10;
}