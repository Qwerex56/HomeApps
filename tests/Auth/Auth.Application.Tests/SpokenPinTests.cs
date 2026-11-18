using Auth.Application.Devices;
using Auth.Application.Devices.IdentifySpeaker;
using Auth.Application.Devices.PairDevice;
using Auth.Application.MyAccount.ClearPin;
using Auth.Application.MyAccount.SetPin;
using Auth.Domain.Identities;
using FluentResults;

namespace Auth.Application.Tests;

/// <summary>jarvis-satellites.md S4: "tu Adrian, PIN 1234" makes a satellite's turn this person's turn.</summary>
public sealed class SpokenPinTests
{
    private readonly World _w = new();
    private readonly Guid _device = Guid.NewGuid();

    private Task<Result> SetPin(string pin, string? password = World.Password) =>
        new SetPinHandler(_w.Identities, _w.CurrentUser, _w.Hasher, _w.Time).Handle(new SetPinCommand(pin, password), default);

    private Task<Result<SpeakerView>> Identify(string pin, params Guid[] candidates) =>
        new IdentifySpeakerHandler(_w.Clients, _w.DeviceIdentities, _w.Identities, _w.Hasher, _w.Time).Handle(
            new IdentifySpeakerCommand("jarvis-service", FakeClients.JarvisServiceSecret, _device, candidates, pin), default);

    private async Task<Identity> WithPin(string email, string pin)
    {
        var (identity, _) = await _w.Registered(email);
        _w.SignInAs(identity);
        Assert.True((await SetPin(pin)).IsSuccess);
        return identity;
    }

    private async Task Paired()
    {
        var paired = await new PairDeviceHandler(_w.Clients, _w.DeviceIdentities, _w.Bus, _w.Time)
            .Handle(new PairDeviceCommand("devices-service", FakeClients.DevicesServiceSecret, _device, Guid.NewGuid(), "salon"), default);
        Assert.True(paired.IsSuccess);
    }

    [Fact]
    public async Task Setting_a_PIN_needs_the_password_and_four_to_eight_digits()
    {
        var (identity, _) = await _w.Registered();
        _w.SignInAs(identity);

        Assert.Equal("auth.invalid_credentials", (await SetPin("1234", "wrong")).Code());
        Assert.Equal("pin.invalid", (await SetPin("12a4")).Code());
        Assert.Equal("pin.invalid", (await SetPin("123")).Code());
        Assert.True((await SetPin("1234")).IsSuccess);
        Assert.Equal("hashed:1234", identity.PinHash);

        await new ClearPinHandler(_w.Identities, _w.CurrentUser, _w.Time).Handle(new ClearPinCommand(), default);
        Assert.False(identity.HasPin);
    }

    [Fact]
    public async Task The_household_member_whose_PIN_matches_is_the_speaker()
    {
        await Paired();
        var adam = await WithPin("adam@example.com", "1234");
        var ewa = await WithPin("ewa@example.com", "9876");

        Assert.Equal(ewa.Id, (await Identify("9876", adam.Id, ewa.Id)).Value.UserId);
        Assert.Null((await Identify("5555", adam.Id, ewa.Id)).Value.UserId);
    }

    [Fact]
    public async Task A_PIN_two_people_share_is_ambiguous_and_not_counted_as_a_miss()
    {
        await Paired();
        var adam = await WithPin("adam@example.com", "1234");
        var ewa = await WithPin("ewa@example.com", "1234");

        Assert.Equal("devices.pin_ambiguous", (await Identify("1234", adam.Id, ewa.Id)).Code());
        Assert.Equal(0, _w.DeviceIdentities.All.Single().PinFailures);
        Assert.Equal(adam.Id, (await Identify("1234", adam.Id)).Value.UserId);
    }

    [Fact]
    public async Task Five_wrong_PINs_lock_the_satellite_for_a_quarter_of_an_hour()
    {
        await Paired();
        var adam = await WithPin("adam@example.com", "1234");

        for (var i = 0; i < 5; i++)
        {
            Assert.Null((await Identify("0000", adam.Id)).Value.UserId);
        }

        Assert.Equal("devices.pin_locked", (await Identify("1234", adam.Id)).Code());
        _w.Time.Now += TimeSpan.FromMinutes(16);
        Assert.Equal(adam.Id, (await Identify("1234", adam.Id)).Value.UserId);
    }

    [Fact]
    public async Task Only_a_service_client_may_ask_and_only_for_an_active_satellite()
    {
        var adam = await WithPin("adam@example.com", "1234");
        var handler = new IdentifySpeakerHandler(_w.Clients, _w.DeviceIdentities, _w.Identities, _w.Hasher, _w.Time);

        var stranger = await handler.Handle(new IdentifySpeakerCommand("jarvis-service", "nope", _device, [adam.Id], "1234"), default);

        Assert.Equal("devices.client_invalid", stranger.Code());
        Assert.Equal("devices.invalid_credentials", (await Identify("1234", adam.Id)).Code());
    }
}
