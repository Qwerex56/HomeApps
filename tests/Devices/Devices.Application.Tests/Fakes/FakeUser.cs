using HomeApps.BuildingBlocks.Application.Abstractions;

namespace Devices.Application.Tests.Fakes;

internal sealed class FakeUser(Guid id) : ICurrentUser
{
    public bool IsAuthenticated => true;

    public Guid? UserId => id;

    public string? Email => null;

    public DateTimeOffset? AuthenticatedAt => null;

    public IReadOnlyCollection<string> Roles => [];

    public bool IsInRole(string role) => false;

    public Guid RequiredUserId => id;
}
