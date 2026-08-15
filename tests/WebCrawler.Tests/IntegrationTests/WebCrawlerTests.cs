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
    public async Task GivenWebpages_WhenCrawl_ThenResultsIncludesAllPages()
    {
        //Arrange
        var pageUrl = "https://example.com/page";
        var handler = new MockHttpMessageHandler("<html>default fallback</html>");

        handler.SetupResponse($"{pageUrl}1", "<html><a href='/page2'>link</a></html>");
        handler.SetupResponse($"{pageUrl}2", "<html><a href='/page3'>link</a><a href='/page4'>link</a></html>");
        handler.SetupResponse($"{pageUrl}3", "<html>no more links</html>");
        handler.SetupResponse($"{pageUrl}4", "<html>no more links</html>");

        using var host = Host.CreateDefaultBuilder().ConfigureServices(services => services
            .AddCrawlerServices()
            .AddHttpClient<IPageFetcher, HttpPageFetcher>()
            .ConfigurePrimaryHttpMessageHandler(() => handler))
            .Build();

        var crawler = host.Services.GetRequiredService<Crawler>();
        //Act
        var result = await crawler.Start($"{pageUrl}1");

        //Assert
        handler.NoOfPagesCalled().ShouldBe(4);
        result.Count.ShouldBe(4);
        result.ShouldContain(new Uri($"{pageUrl}1"));
        result.ShouldContain(new Uri($"{pageUrl}2"));
        result.ShouldContain(new Uri($"{pageUrl}3"));
        result.ShouldContain(new Uri($"{pageUrl}4"));
    }

    [Fact]
    public async Task GivenWebpagesThatHaveCircularReferences_WhenCrawl_ThenResultsDoNotIncludeDuplicates()
    {
        //Arrange
        var pageUrl = "https://example.com/page";
        var handler = new MockHttpMessageHandler("<html>default fallback</html>");

        handler.SetupResponse($"{pageUrl}1", "<html><a href='/page2'>link</a></html>");
        handler.SetupResponse($"{pageUrl}2", "<html><a href='/page3'>link</a><a href='/page4'>link</a></html>");
        handler.SetupResponse($"{pageUrl}3", "<html><a href='/page4'>link</a></html>");
        handler.SetupResponse($"{pageUrl}4", "<html><a href='/page3'>link</a></html>");

        using var host = Host.CreateDefaultBuilder().ConfigureServices(services => services
                .AddCrawlerServices()
                .AddHttpClient<IPageFetcher, HttpPageFetcher>()
                .ConfigurePrimaryHttpMessageHandler(() => handler))
            .Build();

        var crawler = host.Services.GetRequiredService<Crawler>();
        
        //Act
        var result = await crawler.Start($"{pageUrl}1");

        //Assert
        handler.NoOfPagesCalled().ShouldBe(4);
        result.Count.ShouldBe(4);
        result.ShouldContain(new Uri($"{pageUrl}1"));
        result.ShouldContain(new Uri($"{pageUrl}2"));
        result.ShouldContain(new Uri($"{pageUrl}3"));
        result.ShouldContain(new Uri($"{pageUrl}4"));
    }
    
    [Fact]
    public async Task GivenWebpageThatReturnFailsToReturn_WhenCrawl_ThenResultsDoNotIncludePagesLinkedToByBrokenPage()
    {
        //Arrange
        var pageUrl = "https://example.com/page";
        var handler = new MockHttpMessageHandler("<html>default fallback</html>");

        handler.SetupResponse($"{pageUrl}1", "<html><a href='/page2'>link</a></html><a href='/page4'>link</a></html>");
        handler.SetupResponse($"{pageUrl}2", "<html><a href='/page3'>link</a></html><a href='/page5'>link</a></html>", HttpStatusCode.NotFound);
        handler.SetupResponse($"{pageUrl}3", "<html>no more links</html>");
        handler.SetupResponse($"{pageUrl}4", "<html>no more links/html>");

        using var host = Host.CreateDefaultBuilder().ConfigureServices(services => services
                .AddCrawlerServices()
                .AddHttpClient<IPageFetcher, HttpPageFetcher>()
                .ConfigurePrimaryHttpMessageHandler(() => handler))
            .Build();

        var crawler = host.Services.GetRequiredService<Crawler>();
        //Act
        var result = await crawler.Start($"{pageUrl}1");

        //Assert
        handler.NoOfPagesCalled().ShouldBe(3);
        result.Count.ShouldBe(3);
        result.ShouldContain(new Uri($"{pageUrl}1"));
        result.ShouldContain(new Uri($"{pageUrl}2"));
        result.ShouldContain(new Uri($"{pageUrl}4"));
        
        result.ShouldNotContain(new Uri($"{pageUrl}3"));
    }
}