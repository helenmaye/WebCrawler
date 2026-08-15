using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shouldly;
using WebCrawler.Console;
using WebCrawler.Core.Services;
using WebCrawler.Core.Interfaces;

namespace WebCrawler.Tests.IntegrationTests;

public class WebCrawlerTests
{
    [Fact]
    public async Task Fetcher_ReturnsExpectedContent()
    {
        var page1html = "<html><a href='/page2'>link</a></html>";
        var page1url = "https://example.com/page1";
        var page2url = "https://example.com/page2";
        var page3url = "https://example.com/page3";
        var page4url = "https://example.com/page4";
        var handler = new MockHttpMessageHandler("<html>default fallback</html>");

        handler.SetupResponse(page1url, page1html);
        handler.SetupResponse("https://example.com/page2", "<html><a href='/page3'>link</a><a href='/page4'>link</a></html>");
        handler.SetupResponse("https://example.com/page3", "<html>no more links</html>", HttpStatusCode.OK);
        handler.SetupResponse("https://example.com/page4", "<html>no more links</html>", HttpStatusCode.OK);
        handler.SetupResponse("https://example.com/broken", "", HttpStatusCode.InternalServerError);

        var host = Host.CreateDefaultBuilder().ConfigureServices(services => services

        .AddCrawlerServices()
        .AddHttpClient<IPageFetcher, HttpPageFetcher>().ConfigurePrimaryHttpMessageHandler(() => handler)
        .ConfigurePrimaryHttpMessageHandler(() => handler))
        .Build();
    
        var crawler = host.Services.GetRequiredService<Crawler>();
        var result = await crawler.Start(page1url);

        result.Count().ShouldBe(3);
        result.ShouldContain(new Uri(page2url));
        result.ShouldContain(new Uri(page3url));
        result.ShouldContain(new Uri(page4url));
    }
}