using System.Text.Json;
using System.Text.Json.Nodes;
using FluentResults;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Mcp;
using HomeApps.Contracts.Households;
using HomeMix.Application.Abstractions;
using HomeMix.Application.Blocks;
using HomeMix.Application.Data;
using HomeMix.Domain.Data;
using HomeMix.Domain.Installations;
using HomeMix.Domain.Manifests;
using HomeMix.Domain.Tools;

namespace HomeMix.Api.Mcp;

/// <summary>
/// The add-ons' declared tools for one person (docs/homemix.md, "JARVIS"): only installations they reach, that are
/// active and not blocked, and whose installer agreed to JARVIS; only tools their household role allows. Names are
/// <c>{addonId}_{tool}</c>; an add-on in more than one place gets a <c>household</c> argument (a name, an id, or
/// <see cref="AccountName"/>). Nothing is cached: tools/list and every call read the person's places again.
/// </summary>
public sealed class ToolCatalog(
    IInstallationRepository installations,
    IPackageRepository packages,
    IHouseholdRoles households,
    Blocklist blocklist,
    ICurrentUser currentUser)
{
    /// <summary>How the caller's own account is named in the <c>household</c> argument.</summary>
    public const string AccountName = "my account";

    public const string SummaryName = "summary";

    public async Task<IReadOnlyList<CatalogTool>> ListAsync(CancellationToken cancellationToken)
    {
        var tools = new List<CatalogTool>();
        foreach (var places in (await PlacesAsync(cancellationToken)).GroupBy(p => p.Installation.AddonId))
        {
            var manifest = places.First().Manifest;
            if (manifest.Summary is { } summary)
            {
                tools.Add(new CatalogTool(
                    Exposed(manifest, SummaryName), $"{manifest.Name.En}: summary",
                    summary.Description ?? $"What is going on in {manifest.Name.En} ({manifest.Name.Pl}): its {summary.Collection}.",
                    Schema(null, places.Count() > 1), ReadOnly: true, Destructive: false));
            }

            foreach (var tool in manifest.Tools)
            {
                var allowed = places.Where(p => Allows(p, tool)).ToList();
                if (allowed.Count > 0)
                {
                    tools.Add(new CatalogTool(
                        Exposed(manifest, tool.Name), $"{manifest.Name.En}: {tool.Name.Replace('_', ' ')}", tool.Description,
                        Schema(tool, allowed.Count > 1), ReadOnly: !tool.Changes, Destructive: tool.Action == ToolAction.Delete));
                }
            }
        }

        return tools;
    }

    public async Task<Result<ToolCall>> ResolveAsync(
        string name, IReadOnlyDictionary<string, JsonElement>? arguments, CancellationToken cancellationToken)
    {
        var split = name.IndexOf('_', StringComparison.Ordinal);
        if (split <= 0)
        {
            return Result.Fail(ToolErrors.Unknown(name));
        }

        var (addonId, own) = (name[..split], name[(split + 1)..]);
        var places = (await PlacesAsync(cancellationToken)).Where(p => p.Installation.AddonId == addonId).ToList();
        var manifest = places.FirstOrDefault()?.Manifest;
        var tool = manifest?.Tools.FirstOrDefault(t => t.Name == own);
        if (manifest is null || (tool is null && (own != SummaryName || manifest.Summary is null)))
        {
            return Result.Fail(ToolErrors.Unknown(name));
        }

        var allowed = tool is null ? places : places.Where(p => Allows(p, tool)).ToList();
        var args = arguments ?? new Dictionary<string, JsonElement>();
        var household = args.TryGetValue("household", out var h) && h.ValueKind == JsonValueKind.String ? h.GetString() : null;
        var place = NameLookup.Find(
            allowed, household, p => p.Installation.TargetId, p => p.Name, "household", "households_my_households");
        if (place.IsFailed)
        {
            return place.ToResult();
        }

        var unknown = args.Keys.Where(k => k != "household" && k != "key" && tool?.Fields.All(f => f.Name != k) != false).ToList();
        if (unknown.Count > 0)
        {
            return Result.Fail(ToolErrors.Argument($"Unknown argument {string.Join(", ", unknown)}."));
        }

        string? key = null;
        if (tool?.NeedsKey == true)
        {
            key = args.TryGetValue("key", out var k) && k.ValueKind == JsonValueKind.String ? k.GetString() : null;
            if (!DataNames.IsValid(key))
            {
                return Result.Fail(ToolErrors.Argument("key is required: the key of the entry, as list gave it."));
            }
        }

        var fields = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        foreach (var field in tool?.Fields ?? [])
        {
            if (args.TryGetValue(field.Name, out var value) && value.ValueKind != JsonValueKind.Null)
            {
                if (!ToolFieldTypes.Fits(field.Type, value))
                {
                    return Result.Fail(ToolErrors.Argument($"{field.Name} must be a {field.Type}."));
                }

                fields[field.Name] = value;
            }
            else if (tool!.Action == ToolAction.Add && field.Default is { } fallback)
            {
                fields[field.Name] = fallback;
            }
            else if (tool.Action == ToolAction.Add && field.Required)
            {
                return Result.Fail(ToolErrors.Argument($"{field.Name} is required."));
            }
        }

        return new ToolCall(place.Value, tool, key, fields);
    }

    /// <summary>The caller's places with an add-on that gives JARVIS anything, and whose installer agreed to it.</summary>
    private async Task<List<ToolPlace>> PlacesAsync(CancellationToken cancellationToken)
    {
        var me = currentUser.RequiredUserId;
        var mine = await households.GetMyHouseholdsAsync(cancellationToken);
        var found = await installations.ListActiveForAsync([.. mine.Select(h => h.Id)], me, cancellationToken);
        var usable = found.Where(i => i.JarvisAllowed).ToList();
        if (usable.Count == 0)
        {
            return [];
        }

        var byHash = (await packages.FindManyAsync([.. usable.Select(i => i.PackageSha256).Distinct()], cancellationToken))
            .ToDictionary(p => p.Sha256);
        var blocked = await blocklist.SnapshotAsync(cancellationToken);
        var places = new List<ToolPlace>();
        foreach (var installation in usable.Where(i => !blocked(i.AddonId, i.PackageSha256)))
        {
            var manifest = byHash[installation.PackageSha256].Manifest;
            if (manifest.Tools.Count == 0 && manifest.Summary is null)
            {
                continue;
            }

            places.Add(installation.TargetKind == TargetKind.User
                ? new ToolPlace(installation, manifest, AccountName, HouseholdRole.Owner)
                : new ToolPlace(installation, manifest, mine.First(h => h.Id == installation.TargetId).Name,
                    mine.First(h => h.Id == installation.TargetId).Role));
        }

        return places;
    }

    /// <summary>The tool's permission, and for a change of shared data the collection's write role (DataAccess checks it again).</summary>
    private static bool Allows(ToolPlace place, AddonTool tool)
    {
        var permission = tool.Permission is null ? null : place.Manifest.Permissions.FirstOrDefault(p => p.Id == tool.Permission);
        if (permission is not null && Enum.TryParse<HouseholdRole>(permission.MinRole, out var needed) && place.Role < needed)
        {
            return false;
        }

        return !tool.Changes || tool.Scope == DataScope.User || place.Role >= DataAccess.WriteRole(place.Manifest, tool.Collection);
    }

    private static string Exposed(AddonManifest manifest, string name) => $"{manifest.Id}_{name}";

    private static JsonElement Schema(AddonTool? tool, bool household)
    {
        var properties = new JsonObject();
        var required = new JsonArray();
        if (household)
        {
            properties["household"] = new JsonObject
            {
                ["type"] = "string",
                ["description"] = $"Which household: its name or id, or \"{AccountName}\". Required: the add-on is in more than one place.",
            };
            required.Add("household");
        }

        if (tool?.NeedsKey == true)
        {
            properties["key"] = new JsonObject { ["type"] = "string", ["description"] = "The entry's key, as list gives it." };
            required.Add("key");
        }

        foreach (var field in tool?.Fields ?? [])
        {
            var property = new JsonObject { ["type"] = field.Type };
            if (field.Description is not null)
            {
                property["description"] = field.Description;
            }

            if (field.Default is { } fallback)
            {
                property["default"] = JsonNode.Parse(fallback.GetRawText());
            }

            properties[field.Name] = property;
            if (tool!.Action == ToolAction.Add && field.Required && field.Default is null)
            {
                required.Add(field.Name);
            }
        }

        var schema = new JsonObject { ["type"] = "object", ["properties"] = properties };
        if (required.Count > 0)
        {
            schema["required"] = required;
        }

        return JsonSerializer.SerializeToElement(schema);
    }
}
