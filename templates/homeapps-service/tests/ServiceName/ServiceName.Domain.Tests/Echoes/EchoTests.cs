using HomeApps.BuildingBlocks.Domain.Errors;
using ServiceName.Domain.Echoes;

namespace ServiceName.Domain.Tests.Echoes;

public sealed class EchoTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_raises_EchoCreated()
    {
        var owner = Guid.NewGuid();

        var echo = Echo.Create(owner, "  hello ", Now).Value;

        Assert.Equal("hello", echo.Message);
        var raised = Assert.IsType<EchoCreated>(Assert.Single(echo.DomainEvents));
        Assert.Equal(new EchoCreated(echo.Id, owner, "hello"), raised);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_rejects_an_empty_message(string message)
    {
        var result = Echo.Create(Guid.NewGuid(), message, Now);

        Assert.IsType<ValidationError>(Assert.Single(result.Errors));
    }

    [Fact]
    public void An_echo_is_delivered_only_once()
    {
        var echo = Echo.Create(Guid.NewGuid(), "hi", Now).Value;

        Assert.True(echo.MarkDelivered(Now).IsSuccess);
        Assert.IsType<ConflictError>(Assert.Single(echo.MarkDelivered(Now).Errors));
    }

    [Fact]
    public void Only_the_owner_passes_the_ownership_check()
    {
        var owner = Guid.NewGuid();
        var echo = Echo.Create(owner, "hi", Now).Value;

        Assert.True(echo.EnsureOwnedBy(owner).IsSuccess);
        Assert.IsType<ForbiddenError>(Assert.Single(echo.EnsureOwnedBy(Guid.NewGuid()).Errors));
    }
}
