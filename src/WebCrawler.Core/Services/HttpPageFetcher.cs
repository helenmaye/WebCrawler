using WebCrawler.Core.Interfaces;
using WebCrawler.Core.Models;

namespace WebCrawler.Core.Services;

public class HttpPageFetcher(HttpClient httpClient) : IPageFetcher
{
    public async Task<FetchResult> FetchAsync(Uri url)
    {
        try
        {
            using var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseContentRead);

            if (!response.IsSuccessStatusCode)
                return new FetchResult(url)
                {
                    StatusCode = response.StatusCode,
                    Success = false,
                    Error = $"Non-success status code : {(int)response.StatusCode}"
                };

            var html = await response.Content.ReadAsStringAsync();

            return new FetchResult(url)
            {
                StatusCode = response.StatusCode,
                Success = true,
                Html = html
            };
        }
        catch (HttpRequestException ex)
        {
            return new FetchResult(url)
            {
                Success = false,
                Error = $"Exception Occurred {ex.Message}"
            };
        }
    }
}