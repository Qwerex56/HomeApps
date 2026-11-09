using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using HomeApps.Contracts.Notifications;
using Lib.Net.Http.WebPush;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Notifications.Application.Abstractions;
using Notifications.Infrastructure.Push;
using AppPushMessage = Notifications.Application.Abstractions.PushMessage;

namespace Notifications.Infrastructure.Tests;

/// <summary>
/// What goes over the wire to a push service, checked against the RFCs rather than the library: a browser can decrypt
/// it (RFC 8291) and the push service accepts the VAPID signature (RFC 8292). No push service is contacted.
/// </summary>
public sealed class WebPushSenderTests : IDisposable
{
    private static readonly AppPushMessage Message = new(
        "Kasia w Kuchni", "Obiad gotowy 🍝", "/homes/1/apps/chat", "chat", "message", Guid.Parse("0199a3f0-0000-7000-8000-000000000001"),
        "room-42", NotificationUrgency.High);

    private readonly Browser _browser = new();
    private readonly (string Public, string Private) _vapid = VapidKeys.Generate();

    [Fact]
    public async Task A_browser_can_decrypt_the_notification()
    {
        var push = new PushServiceStub();

        var outcome = await Sender(push).SendAsync(_browser.Subscription(), Message, CancellationToken.None);

        Assert.Equal(PushOutcome.Delivered, outcome);
        Assert.Equal("aes128gcm", Assert.Single(push.Request!.Content!.Headers.ContentEncoding));
        using var json = JsonDocument.Parse(_browser.Decrypt(push.Body));
        var root = json.RootElement;
        Assert.Equal("Kasia w Kuchni", root.GetProperty("title").GetString());
        Assert.Equal("Obiad gotowy 🍝", root.GetProperty("body").GetString());
        Assert.Equal("/homes/1/apps/chat", root.GetProperty("url").GetString());
        Assert.Equal("chat", root.GetProperty("source").GetString());
        Assert.Equal("room-42", root.GetProperty("tag").GetString());
    }

    [Fact]
    public async Task The_push_service_gets_a_valid_vapid_signature_and_delivery_hints()
    {
        var push = new PushServiceStub();

        await Sender(push).SendAsync(_browser.Subscription("https://fcm.googleapis.com/fcm/send/abc"), Message, CancellationToken.None);

        var request = push.Request!;
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("86400", Assert.Single(request.Headers.GetValues("TTL")));
        Assert.Equal("high", Assert.Single(request.Headers.GetValues("Urgency")));
        Assert.Equal("room-42", Assert.Single(request.Headers.GetValues("Topic")));

        // Authorization: vapid t=<JWT>, k=<public key>
        var authorization = request.Headers.Authorization!;
        Assert.Equal("vapid", authorization.Scheme);
        var parts = authorization.Parameter!.Split(',', StringSplitOptions.TrimEntries)
            .ToDictionary(p => p[..p.IndexOf('=', StringComparison.Ordinal)], p => p[(p.IndexOf('=', StringComparison.Ordinal) + 1)..]);
        Assert.Equal(_vapid.Public, parts["k"]);

        var jwt = parts["t"].Split('.');
        using var claims = JsonDocument.Parse(Base64Url.Decode(jwt[1]));
        Assert.Equal("https://fcm.googleapis.com", claims.RootElement.GetProperty("aud").GetString());
        Assert.Equal("https://home-apps.dev", claims.RootElement.GetProperty("sub").GetString());
        Assert.True(claims.RootElement.GetProperty("exp").GetInt64() > DateTimeOffset.UtcNow.ToUnixTimeSeconds());

        var key = Base64Url.Decode(_vapid.Public);
        using var verifier = ECDsa.Create(new ECParameters
        {
            Curve = ECCurve.NamedCurves.nistP256,
            Q = new ECPoint { X = key[1..33], Y = key[33..65] },
        });
        Assert.True(verifier.VerifyData(Encoding.ASCII.GetBytes($"{jwt[0]}.{jwt[1]}"), Base64Url.Decode(jwt[2]), HashAlgorithmName.SHA256));
    }

    [Theory]
    [InlineData(HttpStatusCode.Gone, PushOutcome.Gone)]
    [InlineData(HttpStatusCode.NotFound, PushOutcome.Gone)]
    [InlineData(HttpStatusCode.TooManyRequests, PushOutcome.Failed)]
    [InlineData(HttpStatusCode.Forbidden, PushOutcome.Failed)]
    public async Task The_push_service_answer_decides_the_outcome(HttpStatusCode status, PushOutcome expected)
    {
        var outcome = await Sender(new PushServiceStub(status)).SendAsync(_browser.Subscription(), Message, CancellationToken.None);

        Assert.Equal(expected, outcome);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("room-42", "room-42")]
    public void A_safe_short_tag_is_the_topic(string? tag, string? topic) => Assert.Equal(topic, WebPushSender.Topic(tag));

    [Fact]
    public void A_long_or_unsafe_tag_is_hashed_into_a_topic()
    {
        var topic = WebPushSender.Topic("chat:0199a3f0-0000-7000-8000-000000000001");

        Assert.NotNull(topic);
        Assert.Equal(32, topic.Length);
        Assert.Matches("^[A-Za-z0-9_-]+$", topic);
    }

    public void Dispose() => _browser.Dispose();

    private WebPushSender Sender(PushServiceStub push)
    {
#pragma warning disable CA2000 // The client owns the handler; both live as long as the test.
        var client = new PushServiceClient(new HttpClient(push));
#pragma warning restore CA2000
        var keys = new VapidKeys(
            Options.Create(new VapidOptions { PublicKey = _vapid.Public, PrivateKey = _vapid.Private, Subject = "https://home-apps.dev" }),
            new ProductionEnvironment(),
            NullLogger<VapidKeys>.Instance);
        return new WebPushSender(client, keys, NullLogger<WebPushSender>.Instance);
    }
}
