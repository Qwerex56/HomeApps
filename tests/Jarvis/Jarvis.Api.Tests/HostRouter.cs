using System.Collections.Concurrent;

namespace Jarvis.Api.Tests;

/// <summary>Sends each request to the fake source for its host; a host without one does not answer.</summary>
internal sealed class HostRouter(ConcurrentDictionary<string, HttpMessageHandler> hosts) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (!hosts.TryGetValue(request.RequestUri!.Host, out var handler))
        {
            throw new HttpRequestException($"No route to {request.RequestUri.Host}.");
        }

        using var invoker = new HttpMessageInvoker(handler, disposeHandler: false);
        return invoker.SendAsync(request, cancellationToken);
    }
}
