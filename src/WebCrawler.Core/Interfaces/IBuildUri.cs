namespace WebCrawler.Core.Interfaces;

public interface IBuildUri
{
    public Uri? Build(Uri baseUri, string href);
}