using Microsoft.Extensions.DependencyInjection;
using WebCrawler.Core;
using WebCrawler.Core.Interfaces;

namespace WebCrawler.Console;
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCrawlerServices(this IServiceCollection services)
    {
        services.AddScoped<IProcessHTML, ProcessHTML>();
        services.AddScoped<IBuildUri, BuildUri>();
        services.AddScoped<IHtmlFetcher, HtmlFetcher>();
        services.AddHttpClient<IPageFetcher, HttpPageFetcher>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(10);
        });
        services.AddSingleton<Crawler>();

        return services;
    }
}