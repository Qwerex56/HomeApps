using Devices.Domain.Devices;

namespace Devices.Domain.Tests;

public sealed class DeviceNameTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 18, 1, 0, 0, TimeSpan.FromHours(1));

    [Theory]
    [InlineData("mac-biuro")]
    [InlineData("Fotel taty w salonie")]
    [InlineData("Lampka nocna 🌙")]
    [InlineData("Żółć")]
    public void Any_text_is_a_name(string name) => Assert.True(DeviceName.Normalize(name).IsSuccess);

    [Fact]
    public void A_name_is_trimmed() => Assert.Equal("Salon", DeviceName.Normalize("  Salon ").Value);

    [Fact]
    public void Sixty_characters_fit_and_sixty_one_do_not()
    {
        Assert.True(DeviceName.Normalize(new string('a', 60)).IsSuccess);
        Assert.True(DeviceName.Normalize(string.Concat(Enumerable.Repeat("🌙", 60))).IsSuccess);
        Assert.Equal("devices.invalid_name", Code(DeviceName.Normalize(new string('a', 61))));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    [InlineData("two\nlines")]
    public void An_empty_name_or_one_with_control_characters_is_refused(string? name) =>
        Assert.Equal("devices.invalid_name", Code(DeviceName.Normalize(name)));

    [Fact]
    public void Renaming_keeps_the_slug()
    {
        var device = Device.Create(Guid.NewGuid(), "Fotel taty", DeviceKind.Computer, [], Now).Value;

        Assert.True(device.Rename("Fotel dziadka").IsSuccess);

        Assert.Equal("Fotel dziadka", device.Name);
        Assert.Equal("fotel-taty", device.Slug);
    }

    [Fact]
    public void Notes_for_jarvis_are_limited_and_can_be_cleared()
    {
        var device = Device.Create(Guid.NewGuid(), "Fotel", DeviceKind.Computer, [], Now).Value;

        Assert.True(device.SetAdminNotes("  to fotel taty ").IsSuccess);
        Assert.Equal("to fotel taty", device.AdminNotes);
        Assert.Equal("devices.invalid_notes", Code(device.SetAdminNotes(new string('x', 501))));
        Assert.True(device.SetAdminNotes(" ").IsSuccess);
        Assert.Null(device.AdminNotes);
    }

    private static string? Code(FluentResults.ResultBase result) =>
        (result.Errors.SingleOrDefault() as HomeApps.BuildingBlocks.Domain.Errors.AppError)?.Code;
}
