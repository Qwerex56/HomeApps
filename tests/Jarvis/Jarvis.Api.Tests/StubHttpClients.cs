namespace Jarvis.Api.Tests;

/// <summary>An <see cref="IHttpClientFactory"/> with a client per name.</summary>
internal sealed class StubHttpClients(Func<string, HttpClient> create) : IHttpClientFactory
{
    public HttpClient CreateClient(string name) => create(name);
}
