using System.Collections.Concurrent;
using System.Net;
using Shouldly;
using WebCrawler.Core.Models;
using WebCrawler.Core.Services;

namespace WebCrawler.Tests.Services;

public class ResultsWriterTests
{
    private static string CaptureOutput(Action action)
    {
        var originalOut = System.Console.Out;
        try
        {
            using var writer = new StringWriter();
            System.Console.SetOut(writer);
            action();
            return writer.ToString();
        }
        finally
        {
            System.Console.SetOut(originalOut); // always restore, even if action() throws
        }
    }

    [Fact]
    public void GivenSuccessfulResultWithLinks_WhenOutputResults_ThenPrintsPageStatusAndLinks()
    {
        //Arrange
        var results = new ConcurrentDictionary<Uri, CrawlResult>();
        var url = new Uri("https://example.com/page1");
        results.TryAdd(url, new CrawlResult(
            new List<string> { "https://example.com/page2", "https://example.com/page3" },
            HttpStatusCode.OK,
            null));

        //Act
        var output = CaptureOutput(() => ResultsWriter.OutputResults(results));

        //Assert
        output.ShouldContain("Page : https://example.com/page1");
        output.ShouldContain("Status : OK");
        output.ShouldContain("Links found :");
        output.ShouldContain("- https://example.com/page2");
        output.ShouldContain("- https://example.com/page3");
        output.ShouldNotContain("No links found");
    }

    [Fact]
    public void GivenFailedResult_WhenOutputResults_ThenPrintsStatusCodeAndError()
    {
        //Arrange
        var results = new ConcurrentDictionary<Uri, CrawlResult>();
        var url = new Uri("https://example.com/broken");
        results.TryAdd(url, new CrawlResult(null, HttpStatusCode.NotFound, "Non-success status code : 404"));

        //Act
        var output = CaptureOutput(() => ResultsWriter.OutputResults(results));

        //Assert
        output.ShouldContain("Page : https://example.com/broken");
        output.ShouldContain("Status : NotFound - Non-success status code : 404");
    }

    [Fact]
    public void GivenResultWithNullLinks_WhenOutputResults_ThenPrintsNoLinksFoundMessage()
    {
        //Arrange
        var results = new ConcurrentDictionary<Uri, CrawlResult>();
        var url = new Uri("https://example.com/page1");
        results.TryAdd(url, new CrawlResult(null, HttpStatusCode.OK, null));

        //Act
        var output = CaptureOutput(() => ResultsWriter.OutputResults(results));

        //Assert
        output.ShouldContain("No links found");
    }

    [Fact]
    public void GivenResultWithEmptyLinksList_WhenOutputResults_ThenPrintsLinksFoundHeaderWithNoEntries()
    {
        //Arrange
        var results = new ConcurrentDictionary<Uri, CrawlResult>();
        var url = new Uri("https://example.com/page1");
        results.TryAdd(url, new CrawlResult(new List<string>(), HttpStatusCode.OK, null));

        //Act
        var output = CaptureOutput(() => ResultsWriter.OutputResults(results));

        //Assert
        output.ShouldContain("Links found :");
        output.ShouldNotContain("No links found");
        output.ShouldNotContain("- http"); 
    }

    [Fact]
    public void GivenEmptyResults_WhenOutputResults_ThenPrintsNothing()
    {
        //Arrange
        var results = new ConcurrentDictionary<Uri, CrawlResult>();

        //Act
        var output = CaptureOutput(() => ResultsWriter.OutputResults(results));

        //Assert
        output.ShouldBeEmpty();
    }
}