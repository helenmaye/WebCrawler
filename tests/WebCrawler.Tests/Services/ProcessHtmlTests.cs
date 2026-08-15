using AngleSharp;
using NSubstitute;
using Shouldly;
using WebCrawler.Core.Services;
using WebCrawler.Core.Interfaces;

namespace WebCrawler.Tests.Services;

public class ProcessHtmlTests
{
    [Fact]
    public async Task GivenValidHTMLwithLinks_WhenGetLinks_ThenReturnsLinks()
    {
        //Arrange
        var buildUriMock = Substitute.For<IBuildUri>();

        var baseHtml = "https://localhost";
        var html1 = "/pages/link1";
        var html2 = "/pages/link2";
        var html = $"""
                    <html>
                    <head>
                    <title>My Title</title>
                    </head>
                    <body>
                    <p>Paragraph I <a href={html1}>JSON</a></p>
                    <p>Paragraph II <a href='{html2}'>JSON</a></p>
                    </body>
                    </html>
                """;
        
        var uri1 = new Uri($"{baseHtml}{html1}");
        var uri2 = new Uri($"{baseHtml}{html2}");

        buildUriMock.Build(new Uri(baseHtml), html1).Returns(uri1);
        buildUriMock.Build(new Uri(baseHtml), html2).Returns(uri2);

        IProcessHtml subject = new ProcessHtml(buildUriMock);

        //Act
        var result = subject.ExtractLinks(html, new Uri(baseHtml)).ToList();

        //Assert
        result.Count().ShouldBe(2);
        result.FirstOrDefault().ShouldBe(uri1);
        result.LastOrDefault().ShouldBe(uri2);
    }

    [Fact]
    public async Task GivenValidHTMLwithNoLinks_WhenGetLinks_ThenReturnsEmptyEnumerable()
    {
        //Arrange
        var buildUriMock = Substitute.For<IBuildUri>();
        IProcessHtml subject = new ProcessHtml(buildUriMock);
        
        var baseHtml = "https://localhost";
        var html = """
                    <html>
                    <head>
                    <title>My Title</title>
                    </head>
                    <body>
                    <p>Paragraph I</p>
                    <p>Paragraph I</p>
                    </body>
                    </html>
                    """;
        
        var config = Configuration.Default;
        using var context = BrowsingContext.New(config);
        using var doc = await context.OpenAsync(req => req.Content(html));

        //Act
        var result = subject.ExtractLinks(html, new Uri(baseHtml));

        //Assert
        result.Count().ShouldBe(0);
    }
}
