using AngleSharp;
using WebCrawler.Core.Interfaces;

namespace WebCrawler.Core;

public class HtmlFetcher : IHtmlFetcher
{
    public async Task<string?> GetHtmlAsStringAsync(string url)
    {
        var config = Configuration.Default.WithDefaultLoader();
        var context = BrowsingContext.New(config);

        using var document = await context.OpenAsync(url);

        if (document is null)
            return null;

        return document.DocumentElement?.InnerHtml;
    }
}