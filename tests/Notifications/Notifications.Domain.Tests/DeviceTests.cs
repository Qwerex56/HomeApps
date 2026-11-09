using Notifications.Domain.Recipients;

namespace Notifications.Domain.Tests;

public sealed class DeviceTests
{
    [Fact]
    public void The_same_endpoint_keeps_its_device_and_gets_new_keys()
    {
        var recipient = Recipient.Create(Guid.NewGuid());
        var first = recipient.RegisterDevice(A.Target("https://push.example.com/1"), "Chrome", A.Now);
        var renewed = PushTarget.Create("https://push.example.com/1", A.Key(65).Replace('A', 'B'), A.Key(16)).Value;

        var again = recipient.RegisterDevice(renewed, null, A.Now.AddDays(1));

        Assert.Same(first, again);
        Assert.Single(recipient.Devices);
        Assert.Equal(renewed.P256dh, again.P256dh);
        Assert.Equal("Chrome", again.UserAgent);
    }

    [Fact]
    public void Above_the_limit_the_least_recently_used_device_goes()
    {
        var recipient = Recipient.Create(Guid.NewGuid());
        var devices = Enumerable.Range(0, Recipient.MaxDevices)
            .Select(i => recipient.RegisterDevice(A.Target(), null, A.Now.AddMinutes(i)))
            .ToList();
        foreach (var device in devices.Skip(1))
        {
            recipient.DeviceReached(device.Id, A.Now.AddHours(1));
        }

        recipient.RegisterDevice(A.Target(), null, A.Now.AddHours(2));

        Assert.Equal(Recipient.MaxDevices, recipient.Devices.Count);
        Assert.DoesNotContain(devices[0], recipient.Devices);
    }

    [Fact]
    public void Removing_an_unknown_device_is_not_found()
    {
        var recipient = A.RecipientWithDevice();

        Assert.Equal("notifications.device_not_found", recipient.RemoveDevice(Guid.NewGuid()).Code());
        Assert.True(recipient.RemoveDevice(recipient.Devices[0].Id).IsSuccess);
        Assert.Empty(recipient.Devices);
    }

    [Fact]
    public void Forgetting_by_endpoint_gone_and_disabled()
    {
        var recipient = A.RecipientWithDevice();
        var second = recipient.RegisterDevice(A.Target(), null, A.Now);

        Assert.True(recipient.ForgetEndpoint(recipient.Devices[0].Endpoint));
        Assert.False(recipient.ForgetEndpoint("https://push.example.com/unknown"));
        recipient.DeviceGone(second.Id);
        Assert.Empty(recipient.Devices);

        recipient.RegisterDevice(A.Target(), null, A.Now);
        recipient.ForgetDevices();
        Assert.Empty(recipient.Devices);
    }

    [Fact]
    public void A_long_user_agent_is_cut()
    {
        var recipient = Recipient.Create(Guid.NewGuid());

        var device = recipient.RegisterDevice(A.Target(), new string('x', 2000), A.Now);

        Assert.Equal(512, device.UserAgent!.Length);
    }
}
