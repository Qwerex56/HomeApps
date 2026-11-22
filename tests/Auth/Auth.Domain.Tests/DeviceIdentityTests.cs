using Auth.Domain.Devices;

namespace Auth.Domain.Tests;

/// <summary>H-4c item 3: <c>Knows</c> recognises the secret of the last pairing even after <c>Unpair</c>; <c>Accepts</c> also needs it to be active.</summary>
public sealed class DeviceIdentityTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 19, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Knows_the_secret_after_unpairing_while_Accepts_does_not()
    {
        var (identity, secret) = DeviceIdentity.Pair(Guid.NewGuid(), Guid.NewGuid(), "salon", Now);
        identity.Unpair(Now.AddMinutes(1));

        Assert.True(identity.Knows(secret));
        Assert.False(identity.Accepts(secret));
    }

    [Fact]
    public void An_active_identity_both_knows_and_accepts_its_secret()
    {
        var (identity, secret) = DeviceIdentity.Pair(Guid.NewGuid(), Guid.NewGuid(), "salon", Now);

        Assert.True(identity.Knows(secret));
        Assert.True(identity.Accepts(secret));
    }

    [Fact]
    public void The_old_secret_is_not_known_after_pairing_again()
    {
        var (identity, first) = DeviceIdentity.Pair(Guid.NewGuid(), Guid.NewGuid(), "salon", Now);
        identity.Unpair(Now.AddMinutes(1));
        var second = identity.PairAgain(identity.HouseholdId, "salon", Now.AddMinutes(2));

        Assert.False(identity.Knows(first));
        Assert.True(identity.Knows(second));
        Assert.True(identity.Accepts(second));
    }

    [Theory]
    [InlineData("")]
    [InlineData("guess")]
    public void Empty_and_wrong_secrets_are_never_known(string secret)
    {
        var (identity, _) = DeviceIdentity.Pair(Guid.NewGuid(), Guid.NewGuid(), "salon", Now);

        Assert.False(identity.Knows(secret));
    }
}
