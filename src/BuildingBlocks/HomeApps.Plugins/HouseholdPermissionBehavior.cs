using FluentResults;
using HomeApps.Plugins.Manifest;
using MediatR;

namespace HomeApps.Plugins;

/// <summary>
/// Checks every <see cref="IHouseholdRequest"/>: membership first (no access → <c>household.not_found</c>, so outsiders
/// learn nothing), then whether the plugin is on in that household (<c>plugin.disabled</c>), then the role
/// (<c>plugin.forbidden</c>). Rules that depend on the data itself, such as "only the author sees a private list",
/// stay in the domain.
/// </summary>
public sealed class HouseholdPermissionBehavior<TRequest, TResponse>(
    PluginManifest manifest, IHouseholdAccess access, IPluginEnablement enablement)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : ResultBase, new()
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(next);

        if (request is not IHouseholdRequest householdRequest)
        {
            return await next(cancellationToken).ConfigureAwait(false);
        }

        var permission = manifest.FindPermission(householdRequest.Permission)
            ?? throw new InvalidOperationException(
                $"{typeof(TRequest).Name} asks for permission '{householdRequest.Permission}', which the manifest of '{manifest.Id}' does not declare.");

        var role = await access.GetRoleAsync(householdRequest.HouseholdId, cancellationToken).ConfigureAwait(false);
        if (role is null)
        {
            return Fail(PluginErrors.HouseholdNotFound);
        }

        if (!await enablement.IsEnabledAsync(householdRequest.HouseholdId, cancellationToken).ConfigureAwait(false))
        {
            return Fail(PluginErrors.Disabled);
        }

        if (role < permission.MinRole)
        {
            return Fail(PluginErrors.Forbidden);
        }

        return await next(cancellationToken).ConfigureAwait(false);
    }

    private static TResponse Fail(IError error)
    {
        var result = new TResponse();
        result.Reasons.Add(error);
        return result;
    }
}
