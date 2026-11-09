using System.Net;

namespace Notifications.Infrastructure.Tests;

/// <summary>Stands in for a push service (FCM, Mozilla, Apple): keeps the request and answers with a set status.</summary>
internal sealed class PushServiceStub(HttpStatusCode status = HttpStatusCode.Created) : HttpMessageHandler
{
    public HttpRequestMessage? Request { get; private set; }

    public byte[] Body { get; private set; } = [];

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Request = request;
        Body = request.Content is null ? [] : await request.Content.ReadAsByteArrayAsync(cancellationToken);
        return new HttpResponseMessage(status);
    }
}
