using Accounts.Domain.Users;

namespace Accounts.Domain.Tests;

public sealed class AvatarTests
{
    [Fact]
    public void AvatarUrl_AcceptsHttpsAndTheServersOwnPathWithTheUserId()
    {
        var id = Guid.NewGuid();

        Assert.Equal("https://example.com/a.png", AvatarUrl.Create("https://example.com/a.png").Value!.Value);
        Assert.Equal($"/users/v1/{id}/avatar?v=123", AvatarUrl.Create($"/users/v1/{id}/avatar?v=123").Value!.Value);
        Assert.Equal($"/users/v1/{id}/avatar?v=7", AvatarUrl.ForUpload(id, 7).Value);
    }

    [Theory]
    [InlineData("http://example.com/a.png")]
    [InlineData("/users/v1/me/avatar")]
    [InlineData("/other/path")]
    public void AvatarUrl_RejectsOtherAddresses(string raw) =>
        Assert.Equal("profile.avatar_url_invalid", AvatarUrl.Create(raw).Errors[0].Metadata[HomeApps.BuildingBlocks.Domain.Errors.AppError.CodeKey]);
}
