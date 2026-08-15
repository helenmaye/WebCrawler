using WebCrawler.Core.Models;

namespace WebCrawler.Core.Interfaces;

public interface IPageFetcher
{
    public Task<FetchResult> FetchAsync(Uri url);
}