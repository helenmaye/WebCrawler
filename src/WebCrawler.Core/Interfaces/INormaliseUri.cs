namespace WebCrawler.Core.Interfaces;

public interface INormaliseUri
{
    public Uri? Normalise(Uri baseUri, string href);
}