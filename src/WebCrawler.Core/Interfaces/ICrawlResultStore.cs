using WebCrawler.Core.Models;

namespace WebCrawler.Core.Interfaces;

public interface ICrawlResultStore
{
    bool TryAdd(Uri url, CrawlResult result);
    IReadOnlyDictionary<Uri, CrawlResult> GetAll();
    public void Clear();
}