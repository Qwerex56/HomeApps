using System.Net;
using System.Text;

namespace Jarvis.VoiceBridge.Tests;

/// <summary>Devices and Auth without them: one answer per path, and what was asked.</summary>
internal sealed class FakeServer : HttpMessageHandler
{
    public Dictionary<string, (HttpStatusCode Status, string Json)> Answers { get; } = [];

    public List<(string Path, string Body)> Asked { get; } = [];

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var path = request.RequestUri!.AbsolutePath;
        Asked.Add((path, request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken)));
        var (status, json) = Answers.TryGetValue(path, out var answer) ? answer : (HttpStatusCode.NotFound, "{}");
        return new HttpResponseMessage(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
    }
}
