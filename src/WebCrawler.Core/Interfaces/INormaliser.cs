namespace WebCrawler.Core.Interfaces;

public interface INormaliser
{
    public Uri? CreateUri(Uri baseUri, string href);
}