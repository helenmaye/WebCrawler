using WebCrawler.Core.Models;

namespace WebCrawler.Core.Interfaces;

public interface ICrawlResultsStore
{
    bool TryAdd(Uri url, CrawlResult result);
    IReadOnlyDictionary<Uri, CrawlResult> GetAll();
    public void Clear();
}