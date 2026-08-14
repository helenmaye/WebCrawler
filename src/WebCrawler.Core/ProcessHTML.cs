using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using AngleSharp;
using AngleSharp.Html.Dom;
using WebCrawler.Core.Interfaces;

namespace WebCrawler.Core;

public class ProcessHTML(IBuildUri uriBuilder) : IProcessHTML
{
    public IEnumerable<Uri> ExtractLinks( string html, Uri baseUri)
    {
        using var context = BrowsingContext.New(Configuration.Default);
        using var document = context.OpenAsync(req => req.Content(html)).GetAwaiter().GetResult();

        var links = new List<Uri>();

        foreach (var anchor in document.QuerySelectorAll("a[href]"))
        {
            var href = anchor.GetAttribute("href");

            if (!string.IsNullOrWhiteSpace(href))
            {
                var uri = uriBuilder.Build(baseUri, href);
                if (uri != null)
                {
                    yield return uri;
                }
            }
        }
    }

    public IEnumerable<string> ExtractLinks( string html)
    {
        using var context = BrowsingContext.New(Configuration.Default);
        using var document = context.OpenAsync(req => req.Content(html));
        var links = new List<Uri>();

        foreach (var anchor in document.Result.QuerySelectorAll("a[href]"))
        {
            var href = anchor.GetAttribute("href");

            if (!string.IsNullOrWhiteSpace(href))
            {
                yield return href;
            }
        }
    }
}
