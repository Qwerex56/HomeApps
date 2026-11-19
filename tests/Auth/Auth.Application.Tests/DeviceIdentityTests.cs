using Auth.Application.Delegation.DelegateToAssistant;
using Auth.Application.Devices.IssueDeviceToken;
using Auth.Application.Devices.PairDevice;
using Auth.Application.DevicesSync.UnpairDevice;
using HomeApps.Contracts.Devices;
using Microsoft.Extensions.Logging.Abstractions;

namespace Auth.Application.Tests;

/// <summary>jarvis-satellites.md 3a (S13): a household satellite's own identity, its secret and its tokens.</summary>
public sealed class DeviceIdentityTests
{
    private readonly World _w = new();
    private readonly Guid _device = Guid.NewGuid();
    private readonly Guid _household = Guid.NewGuid();

    private Task<FluentResults.Result<Devices.PairedDeviceView>> Pair(string secret = FakeClients.DevicesServiceSecret) =>
        new PairDeviceHandler(_w.Clients, _w.DeviceIdentities, _w.Bus, _w.Time)
            .Handle(new PairDeviceCommand("devices-service", secret, _device, _household, "salon"), default);

    private Task<FluentResults.Result<Devices.DeviceTokenView>> Token(string secret) =>
        new IssueDeviceTokenHandler(_w.DeviceIdentities, _w.Tokens).Handle(new IssueDeviceTokenCommand(_device, secret), default);

    [Fact]
    public async Task Devices_pairs_a_satellite_which_trades_its_secret_for_a_token_of_its_household()
    {
        var paired = await Pair();
        var token = await Token(paired.Value.Secret);

        Assert.Equal(_device, paired.Value.DeviceId);
        Assert.Equal(new HouseholdDevicePaired(_device, _household, "salon"), Assert.Single(_w.Bus.Published));
        Assert.Equal($"device:{_device}:{_household}:self", token.Value.AccessToken);
    }

    [Fact]
    public async Task Only_Devices_with_its_secret_may_pair()
    {
        var refused = await Pair(FakeClients.JarvisServiceSecret);

        Assert.Equal("devices.client_invalid", refused.Code());
        Assert.Empty(_w.DeviceIdentities.All);
    }

    [Fact]
    public async Task Pairing_again_or_unpairing_stops_the_old_secret()
    {
        var first = (await Pair()).Value.Secret;
        var second = (await Pair()).Value.Secret;

        Assert.Equal("devices.invalid_credentials", (await Token(first)).Code());
        Assert.True((await Token(second)).IsSuccess);

        await new UnpairDeviceHandler(_w.DeviceIdentities, _w.Time).Handle(new UnpairDeviceCommand(_device), default);

        // Only the holder of the right secret learns the device was unpaired; the old and empty secrets stay invalid_credentials.
        Assert.Equal("devices.device_unpaired", (await Token(second)).Code());
        Assert.Equal("devices.invalid_credentials", (await Token(first)).Code());
        Assert.Equal("devices.invalid_credentials", (await Token("")).Code());
    }

    [Fact]
    public async Task JARVIS_answering_a_satellite_acts_for_the_satellite()
    {
        await Pair();
        var handler = new DelegateToAssistantHandler(
            _w.Clients, _w.Identities, _w.DeviceIdentities, _w.Tokens, NullLogger<DelegateToAssistantHandler>.Instance);

        var token = await handler.Handle(
            new DelegateToAssistantCommand("jarvis-service", FakeClients.JarvisServiceSecret, _device, "voice", null), default);

        Assert.Equal($"device:{_device}:{_household}:jarvis-service", token.Value.AccessToken);
    }
}
