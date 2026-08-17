using System.Collections.Concurrent;
using WebCrawler.Core.Interfaces;
using WebCrawler.Core.Models;

namespace WebCrawler.Core.DataStores;

public class InMemoryCrawlResultStore : ICrawlResultStore
{
    private readonly ConcurrentDictionary<Uri, CrawlResult> _results = new();

    public bool TryAdd(Uri url, CrawlResult result) => _results.TryAdd(url, result);
    public IReadOnlyDictionary<Uri, CrawlResult> GetAll() => _results;
    public void Clear() => _results.Clear();
}