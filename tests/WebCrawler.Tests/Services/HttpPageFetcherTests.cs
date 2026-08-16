using System.Net;
using Shouldly;
using WebCrawler.Core.Services;
using WebCrawler.Tests.Handlers;
using WebCrawler.Tests.IntegrationTests;

namespace WebCrawler.Tests.Services;

public class HttpPageFetcherTests
{
    private static HttpPageFetcher CreateFetcher(MockHttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = null };
        return new HttpPageFetcher(httpClient);
    }

    [Fact]
    public async Task GivenSuccessfulResponse_WhenFetch_ThenReturnsSuccessWithHtml()
    {
        //Arrange
        var url = new Uri("https://example.com/page1");
        var handler = new MockHttpMessageHandler("<html>fallback</html>");
        handler.SetupResponse(url.ToString(), "<html><body>hello</body></html>");
        var fetcher = CreateFetcher(handler);

        //Act
        var result = await fetcher.FetchAsync(url);

        //Assert
        result.Success.ShouldBeTrue();
        result.StatusCode.ShouldBe(HttpStatusCode.OK);
        result.Html.ShouldBe("<html><body>hello</body></html>");
        result.Error.ShouldBeNull();
    }

    [Theory]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.BadGateway)]
    public async Task GivenNonSuccessStatusCode_WhenFetch_ThenReturnsFailureWithStatusAndError(HttpStatusCode statusCode)
    {
        //Arrange
        var url = new Uri("https://example.com/page1");
        var handler = new MockHttpMessageHandler("<html>fallback</html>");
        handler.SetupResponse(url.ToString(), "<html>error page</html>", statusCode);
        var fetcher = CreateFetcher(handler);

        //Act
        var result = await fetcher.FetchAsync(url);

        //Assert
        result.Success.ShouldBeFalse();
        result.StatusCode.ShouldBe(statusCode);
        result.Error.ShouldBe($"Non-success status code : {(int)statusCode}");
        result.Html.ShouldBeNull(); // body is never read on non-success path
    }

    [Fact]
    public async Task GivenEmptyResponseBody_WhenFetch_ThenReturnsSuccessWithEmptyHtml()
    {
        //Arrange
        var url = new Uri("https://example.com/page1");
        var handler = new MockHttpMessageHandler("<html>fallback</html>");
        handler.SetupResponse(url.ToString(), "");
        var fetcher = CreateFetcher(handler);

        //Act
        var result = await fetcher.FetchAsync(url);

        //Assert
        result.Success.ShouldBeTrue();
        result.Html.ShouldBe("");
    }

    [Fact]
    public async Task GivenHttpRequestExceptionDuringFetch_WhenFetch_ThenReturnsFailureWithExceptionMessage()
    {
        //Arrange
        var url = new Uri("https://example.com/page1");
        var handler = new MockHttpMessageHandler("<html>fallback</html>");
        handler.SetupException(url.ToString(), new HttpRequestException("Connection refused"));
        var fetcher = CreateFetcher(handler);

        //Act
        var result = await fetcher.FetchAsync(url);

        //Assert
        result.Success.ShouldBeFalse();
        result.Error.ShouldBe("Exception Occurred Connection refused");
        result.Html.ShouldBeNull();
        
        result.StatusCode.ShouldBe(null);
    }
}