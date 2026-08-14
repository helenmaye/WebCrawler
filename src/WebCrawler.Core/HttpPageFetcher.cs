using WebCrawler.Core.Interfaces;
using WebCrawler.Core.Models;

namespace WebCrawler.Core;

public class HttpPageFetcher(HttpClient httpClient) : IPageFetcher
{
    public async Task<FetchResult> FetchAsync(Uri url, CancellationToken ct)
    {
        try
        {
            using var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseContentRead, ct);

            if (!response.IsSuccessStatusCode)
                return new FetchResult(url)
                {
                    StatusCode = response.StatusCode,
                    Success = false,
                    Error = $"Non-success status code : {(int)response.StatusCode}"
                };

            var html = await response.Content.ReadAsStringAsync(ct);

            return new FetchResult(url)
            {
                StatusCode = response.StatusCode,
                Success = true,
                Html = html
            };
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            return new FetchResult(url)
            {
                Success = false,
                Error = "Request timed out"
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