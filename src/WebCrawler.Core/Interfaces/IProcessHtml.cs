namespace WebCrawler.Core.Interfaces;
public interface IProcessHtml
{
    public IEnumerable<Uri> ExtractLinks(string html, Uri baseUri);
}