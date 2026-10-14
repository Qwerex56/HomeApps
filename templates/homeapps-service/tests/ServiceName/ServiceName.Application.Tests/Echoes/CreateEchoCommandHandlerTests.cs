using HomeApps.BuildingBlocks.Application.Abstractions;
using NSubstitute;
using ServiceName.Application.Abstractions;
using ServiceName.Application.Echoes.CreateEcho;
using ServiceName.Domain.Echoes;

namespace ServiceName.Application.Tests.Echoes;

public sealed class CreateEchoCommandHandlerTests
{
    private readonly IEchoRepository _echoes = Substitute.For<IEchoRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();

    private CreateEchoCommandHandler CreateHandler() => new(_echoes, _currentUser, _clock);

    [Fact]
    public async Task Adds_an_echo_owned_by_the_caller()
    {
        var userId = Guid.NewGuid();
        _currentUser.RequiredUserId.Returns(userId);

        var result = await CreateHandler().Handle(new CreateEchoCommand("hello"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        _echoes.Received(1).Add(Arg.Is<Echo>(e => e.Id == result.Value && e.OwnerId == userId));
    }

    [Fact]
    public async Task Adds_nothing_when_the_domain_rejects_the_message()
    {
        _currentUser.RequiredUserId.Returns(Guid.NewGuid());

        var result = await CreateHandler().Handle(new CreateEchoCommand(" "), CancellationToken.None);

        Assert.True(result.IsFailed);
        _echoes.DidNotReceiveWithAnyArgs().Add(default!);
    }
}
