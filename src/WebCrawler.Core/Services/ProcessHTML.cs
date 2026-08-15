using AngleSharp;
using WebCrawler.Core.Interfaces;

namespace WebCrawler.Core.Services;

public class ProcessHTML(IBuildUri uriBuilder) : IProcessHTML
{
    public IEnumerable<Uri> ExtractLinks(string html, Uri baseUri)
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
}
