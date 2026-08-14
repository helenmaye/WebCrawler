namespace WebCrawler.Core.Interfaces
{
    public interface IHtmlFetcher
    {
        public Task<string?> GetHtmlAsStringAsync(string url);
    }
}