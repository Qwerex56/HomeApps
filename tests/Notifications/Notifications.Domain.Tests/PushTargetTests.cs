using Notifications.Domain.Recipients;

namespace Notifications.Domain.Tests;

public sealed class PushTargetTests
{
    [Fact]
    public void A_browser_subscription_is_accepted()
    {
        var target = PushTarget.Create("https://fcm.googleapis.com/fcm/send/abc", A.Key(65), A.Key(16));

        Assert.True(target.IsSuccess);
    }

    [Theory]
    [InlineData("http://push.example.com/abc")]
    [InlineData("not a url")]
    [InlineData("")]
    public void The_endpoint_must_be_https(string endpoint) =>
        Assert.Equal("notifications.invalid_endpoint", PushTarget.Create(endpoint, A.Key(65), A.Key(16)).Code());

    [Theory]
    [InlineData(64, 16)]
    [InlineData(65, 15)]
    public void Keys_must_have_the_right_length(int p256dh, int auth) =>
        Assert.Equal("notifications.invalid_keys", PushTarget.Create("https://push.example.com/a", A.Key(p256dh), A.Key(auth)).Code());

    [Fact]
    public void Keys_must_be_base64url() =>
        Assert.Equal("notifications.invalid_keys", PushTarget.Create("https://push.example.com/a", "!!!", A.Key(16)).Code());
}
