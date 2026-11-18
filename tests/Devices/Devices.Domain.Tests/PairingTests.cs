using Devices.Domain.Devices;

namespace Devices.Domain.Tests;

/// <summary>Household satellites pair with a one-time code before they may connect (jarvis-satellites.md 3a).</summary>
public sealed class PairingTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 18, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void A_household_device_cannot_connect_before_it_is_paired()
    {
        var device = Satellite();

        Assert.True(device.IsHouseholdDevice);
        Assert.Equal("devices.not_paired", Code(device.Connect([], Now)));
        Assert.True(device.Pair(Now.AddMinutes(9)).IsSuccess);
        Assert.Null(device.PairingCodeHash);
        Assert.True(device.Connect([], Now.AddMinutes(10)).IsSuccess);
    }

    [Fact]
    public void A_code_works_once_and_only_for_ten_minutes()
    {
        var late = Satellite();
        var used = Satellite();
        used.Pair(Now);

        Assert.Equal("devices.pairing_expired", Code(late.Pair(Now.AddMinutes(10))));
        Assert.Equal("devices.pairing_expired", Code(used.Pair(Now.AddMinutes(1))));
    }

    [Fact]
    public void A_new_code_ends_the_old_pairing()
    {
        var device = Satellite();
        device.Pair(Now);

        device.NewPairingCode(PairingCode.Hash("NEWCODE2"), Now.AddDays(1));

        Assert.Null(device.PairedAt);
        Assert.Equal("devices.not_paired", Code(device.Connect([], Now.AddDays(1))));
    }

    [Fact]
    public void Codes_ignore_case_spaces_and_dashes()
    {
        var code = PairingCode.New();

        Assert.Equal(PairingCode.Length, code.Length);
        Assert.Equal(PairingCode.Hash(code), PairingCode.Hash($" {code[..4].ToLowerInvariant()}-{code[4..]} "));
    }

    [Fact]
    public void Removing_a_paired_satellite_ends_its_pairing_so_restoring_it_needs_a_new_code()
    {
        var satellite = Satellite();
        satellite.Pair(Now);
        Assert.True(satellite.IsPairedSatellite);

        satellite.Disable(Now);
        satellite.Enable();

        Assert.False(satellite.IsPairedSatellite);
        Assert.Null(satellite.PairingCodeHash);
        Assert.Equal("devices.not_paired", Code(satellite.Connect([], Now)));
    }

    private static Device Satellite() =>
        Device.AddToHousehold(Guid.NewGuid(), Guid.NewGuid(), "salon", PairingCode.Hash("ABCDEFGH"), Now).Value;

    private static string? Code(FluentResults.Result result) =>
        (result.Errors.SingleOrDefault() as HomeApps.BuildingBlocks.Domain.Errors.AppError)?.Code;
}
