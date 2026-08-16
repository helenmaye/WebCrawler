namespace WebCrawler.Core.Interfaces;
public interface ILinkExtractor
{
    public IAsyncEnumerable<Uri> AsyncExtractLinks(string html, Uri baseUri);
}