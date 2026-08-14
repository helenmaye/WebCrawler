using AngleSharp.Dom;

namespace WebCrawler.Core.Interfaces;
public interface IProcessHTML
{
    public IEnumerable<Uri> ExtractLinks(string html, Uri baseUri);
    public IEnumerable<string> ExtractLinks( string html);
}