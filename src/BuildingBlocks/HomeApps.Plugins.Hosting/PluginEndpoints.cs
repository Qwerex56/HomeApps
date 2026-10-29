using FluentResults;
using HomeApps.BuildingBlocks.Api.Results;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.Plugins.Manifest;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace HomeApps.Plugins.Hosting;

/// <summary>What every plugin serves besides its own API: the manifest and, with household data, the switch.</summary>
internal static class PluginEndpoints
{
    public static void MapPluginEndpoints(this IEndpointRouteBuilder app, PluginManifest manifest)
    {
        var document = PluginManifestDocument.From(manifest);
        app.MapGet("/manifest", () => TypedResults.Ok(document))
            .AllowAnonymous()
            .WithTags("Plugin")
            .WithName("GetPluginManifest");

        if (manifest.Enablement is null)
        {
            return;
        }

        var enablement = app.MapGroup("/api/households/{householdId:guid}/enablement").WithTags("Plugin");

        enablement.MapGet("/", async (Guid householdId, IHouseholdAccess access, PluginDbContext db, CancellationToken ct) =>
        {
            if (await access.GetRoleAsync(householdId, ct) is null)
            {
                return Result.Fail(PluginErrors.HouseholdNotFound).ToHttpResult();
            }

            var found = await db.PluginEnablements.FindAsync([householdId], ct);
            return Results.Ok(EnablementView.Of(householdId, found));
        })
        .Produces<EnablementView>()
        .WithName("GetPluginEnablement");

        enablement.MapPut("/", async (
            Guid householdId, EnablementRequest body, IHouseholdAccess access, ICurrentUser user, PluginDbContext db,
            TimeProvider time, CancellationToken ct) =>
        {
            var role = await access.GetRoleAsync(householdId, ct);
            if (role is null)
            {
                return Result.Fail(PluginErrors.HouseholdNotFound).ToHttpResult();
            }

            if (role < manifest.Enablement.MinRole)
            {
                return Result.Fail(PluginErrors.Forbidden).ToHttpResult();
            }

            var found = await db.PluginEnablements.FindAsync([householdId], ct);
            if (found is null)
            {
                found = PluginEnablement.For(householdId);
                db.PluginEnablements.Add(found);
            }

            found.Switch(body.Enabled, user.RequiredUserId, time.GetUtcNow());
            await db.SaveChangesAsync(ct);
            return Results.Ok(EnablementView.Of(householdId, found));
        })
        .Produces<EnablementView>()
        .WithName("SetPluginEnablement");
    }
}

public sealed record EnablementRequest(bool Enabled);

/// <param name="ChangedBy">Who last switched it; <c>null</c> while nobody ever did.</param>
public sealed record EnablementView(Guid HouseholdId, bool Enabled, Guid? ChangedBy, DateTimeOffset? ChangedAt)
{
    internal static EnablementView Of(Guid householdId, PluginEnablement? enablement) =>
        new(householdId, enablement?.Enabled ?? false, enablement?.ChangedBy, enablement?.ChangedAt);
}
