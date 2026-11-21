using MacDeviceEmulator.Services;
using Xunit;

namespace MacDeviceEmulator.Tests;

public sealed class ChannelSpeechInputTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void TryWrite_returns_false_for_empty_or_whitespace(string? text)
    {
        var input = new ChannelSpeechInput();
        Assert.False(input.TryWrite(text!));
    }

    [Fact]
    public async Task TryWrite_and_ReadNextAsync_preserves_order_and_timestamp()
    {
        var fixedTime = new DateTimeOffset(2026, 11, 21, 10, 0, 0, TimeSpan.Zero);
        var timeProvider = new TestTimeProvider(fixedTime);
        var input = new ChannelSpeechInput(timeProvider);

        Assert.True(input.TryWrite("Jarvis, włącz światło"));
        timeProvider.Advance(TimeSpan.FromSeconds(5));
        Assert.True(input.TryWrite("Jarvis, która godzina?"));

        var first = await input.ReadNextAsync();
        Assert.NotNull(first);
        Assert.Equal("Jarvis, włącz światło", first.Text);
        Assert.Equal(fixedTime, first.Timestamp);

        var second = await input.ReadNextAsync();
        Assert.NotNull(second);
        Assert.Equal("Jarvis, która godzina?", second.Text);
        Assert.Equal(fixedTime.AddSeconds(5), second.Timestamp);
    }

    [Fact]
    public async Task ReadNextAsync_returns_null_after_Complete()
    {
        var input = new ChannelSpeechInput();
        input.TryWrite("test");
        input.Complete();

        var first = await input.ReadNextAsync();
        Assert.NotNull(first);
        Assert.Equal("test", first.Text);

        var next = await input.ReadNextAsync();
        Assert.Null(next);
    }
}
