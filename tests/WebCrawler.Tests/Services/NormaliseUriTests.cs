using Shouldly;
using WebCrawler.Core.Interfaces;
using WebCrawler.Core.Services;

namespace WebCrawler.Tests.Services;

public class NormaliseUriTests
{
    private readonly INormaliseUri _subject = new NormaliseUri();

    [Theory]
    // relative paths
    [InlineData("https://example.com/page1", "/page2", "https://example.com/page2")]
    [InlineData("https://example.com/page1", "page2", "https://example.com/page2")]
    [InlineData("https://example.com/dir/page1", "page2", "https://example.com/dir/page2")]
    [InlineData("https://example.com/dir/page1", "../page2", "https://example.com/page2")]
    // absolute links on same domain
    [InlineData("https://example.com/page1", "https://example.com/page2", "https://example.com/page2")]
    // protocol-relative
    [InlineData("https://example.com/page1", "//example.com/page2", "https://example.com/page2")]
    // query strings / fragments
    [InlineData("https://example.com/page1", "/page2?foo=bar", "https://example.com/page2?foo=bar")]
    [InlineData("https://example.com/page1", "/page2#section", "https://example.com/page2")] // decide: strip fragment?
    // trailing slash normalization
    [InlineData("https://example.com/page1", "/page2/", "https://example.com/page2")] // decide: normalize?
    // case sensitivity
    [InlineData("https://example.com/page1", "/Page2", "https://example.com/Page2")] // path case usually preserved
    [InlineData("HTTPS://Example.com/page1", "/page2", "https://example.com/page2")] // scheme/host lowercased
    public void ResolvesExpectedUri(string baseUrl, string href, string expected)
    {
        var baseUri = new Uri(baseUrl);

        INormaliseUri subject = new NormaliseUri();
        var result = subject.Normalise(baseUri, href);

        result.ShouldNotBeNull();
        result!.ToString().ShouldBe(expected);
    }

    [Theory]
    // things that should NOT resolve to a crawlable http(s) uri
    [InlineData("mailto:someone@example.com")]
    [InlineData("javascript:void(0)")]
    [InlineData("tel:+441234567890")]
    public void ReturnsNullForNonCrawlableHref(string href)
    {
        var baseUri = new Uri("https://example.com/page1");

        INormaliseUri subject = new NormaliseUri();
        var result = subject.Normalise(baseUri, href);

        result.ShouldBeNull();
    }

    [Fact]
    public void ExternalDomain_StillResolvesButCanBeFilteredBySameOriginCheck()
    {
        // This just confirms BuildUri itself doesn't silently drop external links —
        // same-origin filtering is presumably a separate concern (in Crawler, not BuildUri).
        var baseUri = new Uri("https://example.com/page1");

        INormaliseUri subject = new NormaliseUri();
        var result = subject.Normalise(baseUri, "https://external.com/page2");

        result.ShouldNotBeNull();
        result!.Host.ShouldBe("external.com");
    }
}