using Microsoft.Extensions.DependencyInjection;
using WebCrawler.Core.DataStores;
using WebCrawler.Core.Services;
using WebCrawler.Core.Interfaces;

namespace WebCrawler.Console;
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCrawlerServices(this IServiceCollection services)
    {
        services.AddTransient<ILinkExtractor, LinkExtractor>();
        services.AddTransient<INormaliser, Normaliser>();
        services.AddSingleton<ICrawlResultsStore, InMemoryCrawlResultsStore>();
        services.AddHttpClient<IPageFetcher, HttpPageFetcher>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(10);
        });
        services.AddSingleton<Crawler>();

        return services;
    }
}