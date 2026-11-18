using Devices.Domain.Devices;

namespace Devices.Domain.Tests;

public sealed class DeviceSlugTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 18, 1, 0, 0, TimeSpan.FromHours(1));

    [Theory]
    [InlineData("Fotel taty w salonie", "fotel-taty-w-salonie")]
    [InlineData("Żółta Łódź", "zolta-lodz")]
    [InlineData("  Mac -- biuro!  ", "mac-biuro")]
    [InlineData("mac-biuro", "mac-biuro")]
    [InlineData("🌙", "device")]
    public void The_slug_is_the_name_in_lowercase_ascii_with_dashes(string name, string slug) =>
        Assert.Equal(slug, DeviceSlug.From(name, []));

    [Fact]
    public void A_taken_slug_gets_a_number()
    {
        Assert.Equal("fotel-2", DeviceSlug.From("Fotel", ["fotel"]));
        Assert.Equal("fotel-3", DeviceSlug.From("FOTEL", ["fotel", "fotel-2"]));
    }

    [Fact]
    public void A_long_name_is_cut_and_the_number_still_fits()
    {
        var slug = DeviceSlug.From(new string('a', 60), []);
        Assert.Equal(DeviceSlug.MaxLength, slug.Length);

        var next = DeviceSlug.From(new string('a', 60), [slug]);
        Assert.Equal(DeviceSlug.MaxLength, next.Length);
        Assert.EndsWith("-2", next, StringComparison.Ordinal);
    }

    [Fact]
    public void A_device_gets_its_slug_when_created_and_the_tool_name_uses_it()
    {
        var device = Device.Create(Guid.NewGuid(), "Fotel taty", DeviceKind.Computer, ["fotel-taty"], Now).Value;

        Assert.Equal("fotel-taty-2", device.Slug);
        Assert.Equal("fotel-taty-2_say", device.ToolName(DeviceAction.Create("say", "x", null, false, false).Value));
    }
}
