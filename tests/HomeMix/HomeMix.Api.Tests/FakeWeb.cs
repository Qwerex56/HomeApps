using System.Collections.Concurrent;
using System.Net;
using System.Text;

namespace HomeMix.Api.Tests;

/// <summary>The internet for <c>ctx.fetch</c>: answers set per URL, 404 otherwise, and every URL asked for.</summary>
public sealed class FakeWeb : HttpMessageHandler
{
    private readonly ConcurrentDictionary<string, string> _answers = new(StringComparer.Ordinal);

    public ConcurrentQueue<string> Asked { get; } = new();

    public void Answer(string url, string json) => _answers[url] = json;

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var url = request.RequestUri!.ToString();
        Asked.Enqueue(url);
        return Task.FromResult(_answers.TryGetValue(url, out var json)
            ? new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") }
            : new HttpResponseMessage(HttpStatusCode.NotFound));
    }
}
