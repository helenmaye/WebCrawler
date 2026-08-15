using Microsoft.Extensions.DependencyInjection;
using WebCrawler.Core.Services;
using WebCrawler.Core.Interfaces;

namespace WebCrawler.Console;
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCrawlerServices(this IServiceCollection services)
    {
        services.AddTransient<IProcessHtml, ProcessHtml>();
        services.AddTransient<IBuildUri, BuildUri>();
        services.AddHttpClient<IPageFetcher, HttpPageFetcher>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(10);
        });
        services.AddSingleton<Crawler>();

        return services;
    }
}