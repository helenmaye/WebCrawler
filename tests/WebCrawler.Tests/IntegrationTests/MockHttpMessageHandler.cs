using System.Net;
using System.Text;

public class MockHttpMessageHandler(string defaultContent, HttpStatusCode defaultStatus = HttpStatusCode.OK) : HttpMessageHandler
{
    private readonly Dictionary<string, (HttpStatusCode Status, string Content)> _routes = new();

    public void SetupResponse(string url, string content, HttpStatusCode status = HttpStatusCode.OK)
        => _routes[url] = (status, content);

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var url = request.RequestUri?.ToString() ?? "";

        var (status, content) = _routes.TryGetValue(url, out var match)
            ? match
            : (defaultStatus, defaultContent);

        var response = new HttpResponseMessage(status)
        {
            Content = new StringContent(content, Encoding.UTF8, "text/html"),
            RequestMessage = request
        };

        return Task.FromResult(response);
    }
}