namespace Devices.Application.Common;

/// <summary>Counts frames of one connection in fixed windows; <see cref="TryTake"/> is false once the window's budget is spent.</summary>
public sealed class FrameRateCounter(int limit, TimeSpan window)
{
    private DateTimeOffset _windowStart;
    private int _count;

    public bool TryTake(DateTimeOffset now)
    {
        if (_count == 0 || now - _windowStart >= window)
        {
            _windowStart = now;
            _count = 0;
        }

        if (_count >= limit)
        {
            return false;
        }

        _count++;
        return true;
    }
}
