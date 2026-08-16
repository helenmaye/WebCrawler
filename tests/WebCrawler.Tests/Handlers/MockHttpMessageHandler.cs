using System.Net;
using System.Text;

namespace WebCrawler.Tests.Handlers;

public class MockHttpMessageHandler(
    string defaultContent,
    HttpStatusCode defaultStatus = HttpStatusCode.OK,
    TimeSpan? defaultDelay = null)
    : HttpMessageHandler
{
    private readonly Dictionary<string, (HttpStatusCode Status, string Content, TimeSpan? delay)>
        _routes = new();

    private readonly Dictionary<string, Exception> _exceptions = new();

    public void SetupResponse(string url, string content, HttpStatusCode status = HttpStatusCode.OK,
        TimeSpan? responseDelay = null)
        => _routes[url] = (status, content, responseDelay);

    public void SetupException(string url, Exception exception)
        => _exceptions[url] = exception;

    private int _callCounter;

    public int NoOfPagesCrawled() => _callCounter;

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var url = request.RequestUri?.ToString() ?? "";

        Interlocked.Increment(ref _callCounter);

        if (_exceptions.TryGetValue(url, out var ex))
        {
            throw ex;
        }

        var (status, content, delay) = _routes.TryGetValue(url, out var match)
            ? match
            : (defaultStatus, defaultContent, defaultDelay);

        if (delay != null)
        {
            await Task.Delay(delay.Value, cancellationToken);
        }

        var response = new HttpResponseMessage(status)
        {
            Content = new StringContent(content, Encoding.UTF8),
            RequestMessage = request
        };

        return response;
    }
}