using Devices.Application.Common;

namespace Devices.Application.Tests;

public sealed class FrameRateCounterTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 18, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Frames_above_the_limit_are_refused_until_the_window_passes()
    {
        var counter = new FrameRateCounter(3, TimeSpan.FromMinutes(1));

        Assert.True(counter.TryTake(Now));
        Assert.True(counter.TryTake(Now.AddSeconds(1)));
        Assert.True(counter.TryTake(Now.AddSeconds(2)));
        Assert.False(counter.TryTake(Now.AddSeconds(30)));
        Assert.True(counter.TryTake(Now.AddSeconds(61)));
    }
}
