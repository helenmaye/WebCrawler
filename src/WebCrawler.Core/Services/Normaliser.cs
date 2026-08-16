using WebCrawler.Core.Interfaces;

namespace WebCrawler.Core.Services;
public class Normaliser : INormaliser
{
    private static readonly HashSet<string> AllowedSchemes =
        new (StringComparer.OrdinalIgnoreCase) { "http", "https" };

    public Uri? CreateUri(Uri baseUri, string href)
    {
        if (href.StartsWith("javascript:") || href.StartsWith("tel:") || href.StartsWith("mailto:"))
        {
            return null;
        }
        
        if (href.EndsWith("/"))
        {
            href = href[..^1];
        }

        if (!Uri.TryCreate(baseUri, href, out var absolute) || !AllowedSchemes.Contains(absolute.Scheme))
        {
            return null;
        }

        return string.IsNullOrEmpty(absolute.Fragment) ? absolute : new UriBuilder(absolute) { Fragment = ""}.Uri; 
    }
}