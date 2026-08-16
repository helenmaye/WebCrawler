using AngleSharp;
using WebCrawler.Core.Interfaces;

namespace WebCrawler.Core.Services;

public class LinkExtractor(IBuildUri uriBuilder) : ILinkExtractor
{
    public async IAsyncEnumerable<Uri> AsyncExtractLinks(string html, Uri baseUri)
    {
        using var context = BrowsingContext.New(Configuration.Default);
        using var document = await context.OpenAsync(req => req.Content(html));

        foreach (var anchor in document.QuerySelectorAll("a[href]"))
        {
            var href = anchor.GetAttribute("href");

            if (string.IsNullOrWhiteSpace(href))
            {
                continue;
            }
            var uri = uriBuilder.Build(baseUri, href);
            if (uri != null)
            {
                yield return uri;
            }
        }
    }
}
