using System.Diagnostics.CodeAnalysis;
using AngleSharp;
using AngleSharp.Html.Dom;
using WebCrawler.Core.Interfaces;
using WebCrawler.Core.Models;

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