using Devices.Api.Stream;
using Devices.Application.Abstractions;

namespace Devices.Api.Tests;

/// <summary>H-4c item 9: a <c>state</c> message of a geo device is marked members-only and a guest's stream never gets it. No database needed.</summary>
public sealed class DeviceStreamAccessTests
{
    private readonly Guid _household = Guid.NewGuid();

    [Fact]
    public void A_members_only_state_message_reaches_a_member_stream_and_not_a_guest_stream()
    {
        var stream = new DeviceStream();
        using var guest = stream.Subscribe(_household, members: false);
        using var member = stream.Subscribe(_household, members: true);

        stream.Publish(new DeviceStreamMessage(_household, "state", new { values = 1 }, MembersOnly: true));

        Assert.False(guest.Reader.TryRead(out _));
        Assert.True(member.Reader.TryRead(out var message));
        Assert.Equal("state", message!.Type);
    }

    [Fact]
    public void A_state_message_of_an_ordinary_device_reaches_a_guest_too()
    {
        var stream = new DeviceStream();
        using var guest = stream.Subscribe(_household, members: false);

        stream.Publish(new DeviceStreamMessage(_household, "state", new { values = 1 }));

        Assert.True(guest.Reader.TryRead(out _));
    }
}
