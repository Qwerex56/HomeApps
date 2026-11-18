using Devices.Application.Abstractions;
using FluentResults;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;

namespace Devices.Application.Capabilities.ListRegisteredCapabilities;

internal sealed class ListRegisteredCapabilitiesHandler(ICapabilityRegistry registry, ICurrentUser currentUser)
    : IQueryHandler<ListRegisteredCapabilitiesQuery, IReadOnlyList<RegisteredCapabilityView>>
{
    public async Task<Result<IReadOnlyList<RegisteredCapabilityView>>> Handle(
        ListRegisteredCapabilitiesQuery query, CancellationToken cancellationToken)
    {
        var owner = currentUser.RequireOwner();
        if (owner.IsFailed)
        {
            return owner;
        }

        var all = await registry.ListAsync(cancellationToken);
        return Result.Ok<IReadOnlyList<RegisteredCapabilityView>>([.. all.OrderBy(c => c.Name, StringComparer.Ordinal).Select(c =>
        {
            var definition = c.Definition;
            return new RegisteredCapabilityView(
                c.Name, c.Version, definition.Description, c.Hidden, c.RegisteredAt, c.UpdatedAt,
                [.. definition.Fields.Select(f => new RegisteredCapabilityFieldView(f.Kind.ToString().ToLowerInvariant(), f.Name, f.Required))]);
        })]);
    }
}
