using HomeMix.Domain.Blocks;

namespace HomeMix.Domain.Tests;

public sealed class AddonBlockTests
{
    [Fact]
    public void A_hash_is_kept_in_lowercase_and_an_id_as_it_is()
    {
        var hash = AddonBlock.Create(new string('A', 64), "  ", Guid.NewGuid(), DateTimeOffset.UnixEpoch);
        var id = AddonBlock.Create("pomodoro", " spam ", Guid.NewGuid(), DateTimeOffset.UnixEpoch);

        Assert.Equal(new string('a', 64), hash.Value);
        Assert.Null(hash.Reason);
        Assert.Equal("pomodoro", id.Value);
        Assert.Equal("spam", id.Reason);
    }

    [Theory]
    [InlineData("pomodoro", false)]
    [InlineData("0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef", true)]
    [InlineData("0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdeg", false)]
    public void A_sha256_is_64_hex_characters(string value, bool expected) => Assert.Equal(expected, AddonBlock.IsSha256(value));
}
