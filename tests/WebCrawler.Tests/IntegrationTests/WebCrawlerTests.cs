using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shouldly;
using WebCrawler.Console;
using WebCrawler.Core.Services;
using WebCrawler.Core.Interfaces;
using WebCrawler.Core.Models;
using WebCrawler.Tests.Handlers;

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

        var expectedResults = new ConcurrentDictionary<Uri, CrawlResult>();
        expectedResults.TryAdd(new Uri($"{pageUrl}1"),
            new CrawlResult(new List<string> { $"{pageUrl}2" }, HttpStatusCode.OK, null));
        expectedResults.TryAdd(new Uri($"{pageUrl}2"),
            new CrawlResult(new List<string> { $"{pageUrl}3", $"{pageUrl}4" }, HttpStatusCode.OK, null));
        expectedResults.TryAdd(new Uri($"{pageUrl}3"), new CrawlResult(null, HttpStatusCode.OK, null));
        expectedResults.TryAdd(new Uri($"{pageUrl}4"), new CrawlResult(null, HttpStatusCode.OK, null));

        using var host = Host.CreateDefaultBuilder().ConfigureServices(services => services
                .AddCrawlerServices()
                .AddHttpClient<IPageFetcher, HttpPageFetcher>()
                .ConfigurePrimaryHttpMessageHandler(() => handler))
            .Build();

        var crawler = host.Services.GetRequiredService<Crawler>();
        //Act
        var results = await crawler.Start($"{pageUrl}1");

        //Assert
        handler.NoOfPagesCrawled().ShouldBe(4);
        results.ShouldBeEquivalentTo(expectedResults);
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

        var expectedResults = new ConcurrentDictionary<Uri, CrawlResult>();
        expectedResults.TryAdd(new Uri($"{pageUrl}1"),
            new CrawlResult(new List<string> { $"{pageUrl}2" }, HttpStatusCode.OK, null));
        expectedResults.TryAdd(new Uri($"{pageUrl}2"),
            new CrawlResult(new List<string> { $"{pageUrl}3", $"{pageUrl}4" }, HttpStatusCode.OK, null));
        expectedResults.TryAdd(new Uri($"{pageUrl}3"),
            new CrawlResult(new List<string> { $"{pageUrl}4" }, HttpStatusCode.OK, null));
        expectedResults.TryAdd(new Uri($"{pageUrl}4"),
            new CrawlResult(new List<string> { $"{pageUrl}3" }, HttpStatusCode.OK, null));

        using var host = Host.CreateDefaultBuilder().ConfigureServices(services => services
                .AddCrawlerServices()
                .AddHttpClient<IPageFetcher, HttpPageFetcher>()
                .ConfigurePrimaryHttpMessageHandler(() => handler))
            .Build();

        var crawler = host.Services.GetRequiredService<Crawler>();

        //Act
        var results = await crawler.Start($"{pageUrl}1");

        //Assert
        handler.NoOfPagesCrawled().ShouldBe(4);
        results.ShouldBeEquivalentTo(expectedResults);
    }

    [Fact]
    public async Task GivenWebpageThatReturnFailsToReturn_WhenCrawl_ThenResultsDoNotIncludePagesLinkedToByBrokenPage()
    {
        //Arrange
        var pageUrl = "https://example.com/page";
        var handler = new MockHttpMessageHandler("<html>default fallback</html>");

        handler.SetupResponse($"{pageUrl}1", "<html><a href='/page2'>link</a></html><a href='/page4'>link</a></html>");
        handler.SetupResponse($"{pageUrl}2", "<html><a href='/page3'>link</a></html><a href='/page5'>link</a></html>",
            HttpStatusCode.NotFound);
        handler.SetupResponse($"{pageUrl}3", "<html>no more links</html>");
        handler.SetupResponse($"{pageUrl}4", "<html>no more links/html>");

        var expectedResults = new ConcurrentDictionary<Uri, CrawlResult>();
        expectedResults.TryAdd(new Uri($"{pageUrl}1"),
            new CrawlResult(new List<string> { $"{pageUrl}2", $"{pageUrl}4" }, HttpStatusCode.OK, null));
        expectedResults.TryAdd(new Uri($"{pageUrl}2"),
            new CrawlResult(null, HttpStatusCode.NotFound, "Non-success status code : 404"));
        expectedResults.TryAdd(new Uri($"{pageUrl}4"), new CrawlResult(null, HttpStatusCode.OK, null));

        using var host = Host.CreateDefaultBuilder().ConfigureServices(services => services
                .AddCrawlerServices()
                .AddHttpClient<IPageFetcher, HttpPageFetcher>()
                .ConfigurePrimaryHttpMessageHandler(() => handler))
            .Build();

        var crawler = host.Services.GetRequiredService<Crawler>();
        //Act
        var results = await crawler.Start($"{pageUrl}1");

        //Assert
        handler.NoOfPagesCrawled().ShouldBe(3);
        results.ShouldBeEquivalentTo(expectedResults);
    }

    [Fact]
    public async Task GivenWebpageThatLinksToTheSamePageMultipleTimes_WhenCrawl_ThenResultsDoNotIncludeDuplicates()
    {
        //Arrange
        var pageUrl = "https://example.com/page";
        var handler = new MockHttpMessageHandler("<html>default fallback</html>");

        handler.SetupResponse($"{pageUrl}1", "<html><a href='/page2'>link</a><a href='/page2'>link</a></html>");
        handler.SetupResponse($"{pageUrl}2",
            "<html><a href='/page3'>link</a><a href='/page1'>link</a><a href='/page3'>link</a></html>");
        handler.SetupResponse($"{pageUrl}3", "<html><a href='/page3'>link</a></html>");

        var expectedResults = new ConcurrentDictionary<Uri, CrawlResult>();
        expectedResults.TryAdd(new Uri($"{pageUrl}1"),
            new CrawlResult(new List<string> { $"{pageUrl}2" }, HttpStatusCode.OK, null));
        expectedResults.TryAdd(new Uri($"{pageUrl}2"),
            new CrawlResult(new List<string> { $"{pageUrl}3", $"{pageUrl}1" }, HttpStatusCode.OK, null));
        expectedResults.TryAdd(new Uri($"{pageUrl}3"),
            new CrawlResult(new List<string> { $"{pageUrl}3" }, HttpStatusCode.OK, null));

        using var host = Host.CreateDefaultBuilder().ConfigureServices(services => services
                .AddCrawlerServices()
                .AddHttpClient<IPageFetcher, HttpPageFetcher>()
                .ConfigurePrimaryHttpMessageHandler(() => handler))
            .Build();

        var crawler = host.Services.GetRequiredService<Crawler>();
        //Act
        var results = await crawler.Start($"{pageUrl}1");

        //Assert
        handler.NoOfPagesCrawled().ShouldBe(3);
        results.ShouldBeEquivalentTo(expectedResults);
    }

    [Fact]
    public async Task GivenMultipleSlowPages_WhenCrawl_ThenPagesAreFetchedConcurrentlyNotSequentially()
    {
        //Arrange
        var pageUrl = "https://example.com/page";
        var handler = new MockHttpMessageHandler("<html>default fallback</html>");

        var pageDelay = TimeSpan.FromMilliseconds(200);
        const int pageCount = 8;

        // Page 1 links out to all the others; each of those has no further links.
        var linksFromPage1 = Enumerable.Range(2, pageCount - 1)
            .Select(i => $"<a href='/page{i}'>link</a>")
            .Aggregate((a, b) => a + b);

        handler.SetupResponse($"{pageUrl}1", $"<html>{linksFromPage1}</html>", HttpStatusCode.OK, pageDelay);
        for (var i = 2; i <= pageCount; i++)
        {
            handler.SetupResponse($"{pageUrl}{i}", "<html>no more links</html>", HttpStatusCode.OK, pageDelay);
        }

        using var host = Host.CreateDefaultBuilder().ConfigureServices(services => services
                .AddCrawlerServices()
                .AddHttpClient<IPageFetcher, HttpPageFetcher>()
                .ConfigurePrimaryHttpMessageHandler(() => handler))
            .Build();

        var crawler = host.Services.GetRequiredService<Crawler>();

        //Act
        var stopwatch = Stopwatch.StartNew();
        var results = await crawler.Start($"{pageUrl}1");
        stopwatch.Stop();

        //Assert
        handler.NoOfPagesCrawled().ShouldBe(pageCount);
        results.Count.ShouldBe(pageCount);

        // Sequential fetching would take roughly pageCount * pageDelay (here: 8 * 200ms = 1.6s).
        // Concurrent fetching should take roughly 2 * pageDelay (page1, then its children in parallel),
        // plus scheduling overhead. We assert well below the sequential bound to prove the crawler
        // isn't just correct, but actually overlapping I/O.
        var sequentialBound = TimeSpan.FromMilliseconds(pageDelay.TotalMilliseconds * pageCount);
        var concurrentCeiling =
            TimeSpan.FromMilliseconds(pageDelay.TotalMilliseconds * 4); // generous slack for scheduling

        stopwatch.Elapsed.ShouldBeLessThan(concurrentCeiling,
            $"expected concurrent execution (~{pageDelay.TotalMilliseconds * 2}ms) but took {stopwatch.ElapsedMilliseconds}ms, " +
            $"suggesting pages were fetched sequentially (would be ~{sequentialBound.TotalMilliseconds}ms)");
    }

    [Fact]
    public async Task GivenPageLinksToDifferentHost_WhenCrawl_ThenExternalLinkIsAddedToListOfLinksButNotCrawled()
    {
        //Arrange
        var pageUrl = "https://example.com/page";
        var handler = new MockHttpMessageHandler("<html>default fallback</html>");

        handler.SetupResponse($"{pageUrl}1", "<html><a href='/page2'>internal</a><a href='https://other-domain.com/page1'>external</a></html>");
        handler.SetupResponse($"{pageUrl}2", "<html>no more links</html>");

        var expectedResults = new ConcurrentDictionary<Uri, CrawlResult>();
        expectedResults.TryAdd(new Uri($"{pageUrl}1"), new CrawlResult(new List<string> { $"{pageUrl}2", "https://other-domain.com/page1" }, HttpStatusCode.OK, null));
        expectedResults.TryAdd(new Uri($"{pageUrl}2"), new CrawlResult(null, HttpStatusCode.OK, null));

        using var host = Host.CreateDefaultBuilder().ConfigureServices(services => services
                .AddCrawlerServices()
                .AddHttpClient<IPageFetcher, HttpPageFetcher>()
                .ConfigurePrimaryHttpMessageHandler(() => handler))
            .Build();

        var crawler = host.Services.GetRequiredService<Crawler>();

        //Act
        var results = await crawler.Start($"{pageUrl}1");

        //Assert
        handler.NoOfPagesCrawled().ShouldBe(2); // external host never fetched
        results.ShouldBeEquivalentTo(expectedResults);
    }
    
    [Fact]
    public async Task GivenLinkWithQueryString_WhenCrawl_ThenTreatedAsDistinctPageFromBaseUrl()
    {
        // Design decision: query strings can identify meaningfully different content
        // (pagination, filters, IDs), so /page2 and /page2?x=1 are treated as distinct
        // pages rather than collapsed. 

        //Arrange
        var pageUrl = "https://example.com/page";
        var handler = new MockHttpMessageHandler("<html>no more links</html>");

        handler.SetupResponse($"{pageUrl}1",
            "<html><a href='/page2'>base</a><a href='/page2?x=1'>with query</a></html>");

        using var host = Host.CreateDefaultBuilder().ConfigureServices(services => services
                .AddCrawlerServices()
                .AddHttpClient<IPageFetcher, HttpPageFetcher>()
                .ConfigurePrimaryHttpMessageHandler(() => handler))
            .Build();

        var crawler = host.Services.GetRequiredService<Crawler>();

        //Act
        var results = await crawler.Start($"{pageUrl}1");

        //Assert
        results.ShouldContainKey(new Uri($"{pageUrl}2"));
        results.ShouldContainKey(new Uri($"{pageUrl}2?x=1"));
        handler.NoOfPagesCrawled().ShouldBe(3); // page1, page2, page2?x=1 — fetched separately
    }

    [Fact]
    public async Task GivenLinksWithVariousUrlForms_WhenCrawl_ThenEquivalentUrlsAreTreatedAsSamePage()
    {
        // ASSUMPTION: relative paths, protocol-relative URLs, trailing slashes, and query/fragment
        // variants that point at the "same" resource are normalized to one canonical Uri 

        //Arrange
        var pageUrl = "https://example.com/page";
        var handler = new MockHttpMessageHandler("<html>no more links</html>");

        handler.SetupResponse($"{pageUrl}1",
            "<html>" +
            "<a href='/page2'>absolute path</a>" +
            "<a href='page2'>relative path</a>" +
            "<a href='//example.com/page2'>protocol-relative</a>" +
            "<a href='/page2/'>trailing slash</a>" +
            "<a href='/page2#section'>with fragment</a>" +
            "</html>");

        using var host = Host.CreateDefaultBuilder().ConfigureServices(services => services
                .AddCrawlerServices()
                .AddHttpClient<IPageFetcher, HttpPageFetcher>()
                .ConfigurePrimaryHttpMessageHandler(() => handler))
            .Build();

        var crawler = host.Services.GetRequiredService<Crawler>();

        //Act
        var results = await crawler.Start($"{pageUrl}1");

        //Assert
        results.Keys.Count(k => k.ToString().StartsWith($"{pageUrl}2")).ShouldBe(1);
    }

    [Fact]
    public async Task GivenPageThatLinksDirectlyToItself_WhenCrawl_ThenPageIsNotCrawledTwice()
    {
        //Arrange
        var pageUrl = "https://example.com/page";
        var handler = new MockHttpMessageHandler("<html>default fallback</html>");

        handler.SetupResponse($"{pageUrl}1", "<html><a href='/page1'>self</a></html>");

        var expectedResults = new ConcurrentDictionary<Uri, CrawlResult>();
        expectedResults.TryAdd(new Uri($"{pageUrl}1"),
            new CrawlResult(new List<string> { $"{pageUrl}1" }, HttpStatusCode.OK, null));

        using var host = Host.CreateDefaultBuilder().ConfigureServices(services => services
                .AddCrawlerServices()
                .AddHttpClient<IPageFetcher, HttpPageFetcher>()
                .ConfigurePrimaryHttpMessageHandler(() => handler))
            .Build();

        var crawler = host.Services.GetRequiredService<Crawler>();

        //Act
        var results = await crawler.Start($"{pageUrl}1");

        //Assert
        handler.NoOfPagesCrawled().ShouldBe(1); // fetched once, not re-queued on self-link
        results.ShouldBeEquivalentTo(expectedResults);
    }

    [Theory]
    [InlineData("")]
    [InlineData("<html></html>")]
    [InlineData("<html><body>no links here</body></html>")]
    [InlineData("<html><body><a href='/page2'>unclosed")]
    [InlineData("<a>no href attribute</a>")]
    public async Task GivenMalformedOrEmptyHtml_WhenCrawl_ThenCrawlCompletesWithoutError(string html)
    {
        //Arrange
        var pageUrl = "https://example.com/page1";
        var handler = new MockHttpMessageHandler("<html>default fallback</html>");
        handler.SetupResponse(pageUrl, html);

        using var host = Host.CreateDefaultBuilder().ConfigureServices(services => services
                .AddCrawlerServices()
                .AddHttpClient<IPageFetcher, HttpPageFetcher>()
                .ConfigurePrimaryHttpMessageHandler(() => handler))
            .Build();

        var crawler = host.Services.GetRequiredService<Crawler>();

        //Act
        var results = await crawler.Start(pageUrl);

        //Assert
        results.ShouldContainKey(new Uri(pageUrl));
        results[new Uri(pageUrl)].StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GivenPageThatThrowsDuringFetch_WhenCrawl_ThenResultRecordsFailureWithoutCrashingCrawl()
    {
        // ASSUMPTION: HttpPageFetcher.FetchAsync catches HttpRequestException/TaskCanceledException
        // internally and returns a CrawlResult-shaped failure (same pattern as your 404 test),
        // rather than letting the exception propagate up through Crawler and killing the whole run.
        // If FetchAsync currently lets exceptions bubble up, this test will fail with an unhandled
        // exception rather than a clean assertion failure - which itself is useful signal that
        // Crawler needs a try/catch around the fetch call per-worker so one bad page doesn't
        // take down the whole crawl.
    
        //Arrange
        var pageUrl = "https://example.com/page";
        var handler = new MockHttpMessageHandler("<html>default fallback</html>");
    
        handler.SetupResponse($"{pageUrl}1", "<html><a href='/page2'>link</a><a href='/page3'>link</a></html>");
        handler.SetupException($"{pageUrl}2", new HttpRequestException("Connection refused"));
        handler.SetupResponse($"{pageUrl}3", "<html>no more links</html>");
    
        using var host = Host.CreateDefaultBuilder().ConfigureServices(services => services
                .AddCrawlerServices()
                .AddHttpClient<IPageFetcher, HttpPageFetcher>()
                .ConfigurePrimaryHttpMessageHandler(() => handler))
            .Build();
    
        var crawler = host.Services.GetRequiredService<Crawler>();
    
        //Act
        var results = await crawler.Start($"{pageUrl}1");
    
        //Assert
        results.ShouldContainKey(new Uri($"{pageUrl}3")); // sibling page still crawled fine
        results[new Uri($"{pageUrl}2")].StatusCode.ShouldNotBe(HttpStatusCode.OK);
        results[new Uri($"{pageUrl}2")].Error.ShouldNotBeNullOrEmpty();
    }
}