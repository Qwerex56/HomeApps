using System.Text.Json;
using System.Text.RegularExpressions;
using FluentResults;
using HomeMix.Domain.Installations;

namespace HomeMix.Domain.Manifests;

/// <summary>One pass over homemix.json that collects every problem; used by <see cref="AddonManifestParser"/>.</summary>
internal sealed partial class ManifestReader(JsonElement root)
{
    private static readonly HashSet<string> Roles = new(StringComparer.Ordinal) { "Guest", "Member", "Admin", "Owner" };
    private static readonly HashSet<string> Sizes = new(StringComparer.Ordinal) { "Small", "Medium", "Large" };
    private static readonly HashSet<string> Places = new(StringComparer.Ordinal) { "Household", "Wall", "Account" };

    [GeneratedRegex("^[a-z][a-z0-9-]{1,30}$")]
    private static partial Regex IdPattern();

    [GeneratedRegex("^[a-z][a-z0-9._-]{0,63}$")]
    private static partial Regex NamePattern();

    [GeneratedRegex("^[A-Za-z0-9._/-]{0,200}$")]
    private static partial Regex PathPattern();

    private readonly List<string> _problems = [];

    public Result<AddonManifest> Read()
    {
        var version = Int("homemixVersion");
        if (version != AddonManifestParser.SupportedVersion)
        {
            _problems.Add($"homemixVersion must be {AddonManifestParser.SupportedVersion}.");
        }

        var id = String("id", required: true) ?? string.Empty;
        if (id.Length > 0 && !IdPattern().IsMatch(id))
        {
            _problems.Add("id must be 2-31 characters: lowercase letters, digits and '-', starting with a letter.");
        }

        var addonVersion = String("version", required: true) ?? string.Empty;
        if (addonVersion.Length > 0 && !AddonVersion.IsValid(addonVersion))
        {
            _problems.Add("version must look like 1.0.0.");
        }

        var requires = root.TryGetProperty("requires", out var r) && r.ValueKind == JsonValueKind.Object
            && r.TryGetProperty("homeApps", out var h) && h.ValueKind == JsonValueKind.String
            ? h.GetString()!
            : null;
        if (!AddonVersion.IsValid(requires))
        {
            _problems.Add("requires.homeApps must be a version such as 2.3.0.");
        }

        var name = Text(root, "name", required: true);
        var description = Text(root, "description", required: false);
        var icon = String("icon", required: false) ?? "pi-box";
        if (!icon.StartsWith("pi-", StringComparison.Ordinal) || icon.Length > 64)
        {
            _problems.Add("icon must be a PrimeIcons name such as pi-stopwatch.");
        }

        var targets = Targets();
        var entry = String("entry", required: true) ?? string.Empty;
        CheckHtmlPath("entry", entry);

        var pages = Pages(name);
        var widgets = Widgets();
        var capabilities = Strings("capabilities");
        foreach (var unknown in capabilities.Where(c => !AddonCapabilities.All.Contains(c)))
        {
            _problems.Add($"capabilities: '{unknown}' is not a capability the bridge offers.");
        }

        var network = Strings("network");
        foreach (var origin in network.Where(o => !IsHttpsOrigin(o)))
        {
            _problems.Add($"network: '{origin}' must be an https origin without a path or wildcards, e.g. https://api.open-meteo.com.");
        }

        var permissions = Permissions();
        var jarvis = root.TryGetProperty("jarvis", out var j) && j.ValueKind == JsonValueKind.True;

        if (_problems.Count > 0)
        {
            return Result.Fail(_problems.Select(ManifestErrors.Invalid));
        }

        return new AddonManifest(
            id, addonVersion, requires!, name!, description, icon, targets, entry, pages, widgets,
            capabilities.Distinct(StringComparer.Ordinal).ToList(),
            network.Select(o => o.TrimEnd('/')).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
            permissions, jarvis);
    }

    private List<TargetKind> Targets()
    {
        if (!root.TryGetProperty("targets", out var element))
        {
            return [TargetKind.Household, TargetKind.User];
        }

        var targets = new List<TargetKind>();
        foreach (var value in StringArray(element, "targets"))
        {
            if (Enum.TryParse<TargetKind>(value, ignoreCase: false, out var kind) && Enum.IsDefined(kind))
            {
                targets.Add(kind);
            }
            else
            {
                _problems.Add($"targets: '{value}' must be Household or User.");
            }
        }

        if (targets.Count == 0)
        {
            _problems.Add("targets must name at least one of Household, User (or be left out for both).");
        }

        return targets.Distinct().ToList();
    }

    private List<AddonPage> Pages(LocalizedText? name)
    {
        if (!root.TryGetProperty("pages", out var element))
        {
            return [new AddonPage(string.Empty, name ?? new LocalizedText(string.Empty, string.Empty), Menu: true)];
        }

        var pages = new List<AddonPage>();
        foreach (var page in Objects(element, "pages"))
        {
            var path = page.TryGetProperty("path", out var p) && p.ValueKind == JsonValueKind.String ? p.GetString()! : string.Empty;
            if (!PathPattern().IsMatch(path) || path.Contains("..", StringComparison.Ordinal))
            {
                _problems.Add($"pages: '{path}' is not a valid page path.");
            }

            var title = Text(page, "title", required: true, prefix: $"pages['{path}'].");
            var menu = !page.TryGetProperty("menu", out var m) || m.ValueKind != JsonValueKind.False;
            pages.Add(new AddonPage(path, title ?? name!, menu));
        }

        if (pages.Select(p => p.Path).Distinct(StringComparer.Ordinal).Count() != pages.Count)
        {
            _problems.Add("pages: every path must be different.");
        }

        return pages;
    }

    private List<AddonWidget> Widgets()
    {
        var widgets = new List<AddonWidget>();
        if (!root.TryGetProperty("widgets", out var element))
        {
            return widgets;
        }

        foreach (var widget in Objects(element, "widgets"))
        {
            var id = widget.TryGetProperty("id", out var i) && i.ValueKind == JsonValueKind.String ? i.GetString()! : string.Empty;
            if (!NamePattern().IsMatch(id))
            {
                _problems.Add($"widgets: '{id}' is not a valid widget id.");
            }

            var title = Text(widget, "title", required: true, prefix: $"widgets['{id}'].");
            var size = widget.TryGetProperty("size", out var s) && s.ValueKind == JsonValueKind.String ? s.GetString()! : "Medium";
            if (!Sizes.Contains(size))
            {
                _problems.Add($"widgets['{id}'].size must be Small, Medium or Large.");
            }

            var places = widget.TryGetProperty("places", out var pl) ? StringArray(pl, $"widgets['{id}'].places") : ["Household"];
            foreach (var place in places.Where(p => !Places.Contains(p)))
            {
                _problems.Add($"widgets['{id}'].places: '{place}' must be Household, Wall or Account.");
            }

            string? entry = null;
            if (widget.TryGetProperty("entry", out var e) && e.ValueKind == JsonValueKind.String)
            {
                entry = e.GetString()!;
                CheckHtmlPath($"widgets['{id}'].entry", entry);
            }

            widgets.Add(new AddonWidget(id, title ?? new LocalizedText(id, id), size, places, entry));
        }

        if (widgets.Select(w => w.Id).Distinct(StringComparer.Ordinal).Count() != widgets.Count)
        {
            _problems.Add("widgets: every id must be different.");
        }

        return widgets;
    }

    private List<AddonPermission> Permissions()
    {
        var permissions = new List<AddonPermission>();
        if (!root.TryGetProperty("permissions", out var element))
        {
            return permissions;
        }

        foreach (var permission in Objects(element, "permissions"))
        {
            var id = permission.TryGetProperty("id", out var i) && i.ValueKind == JsonValueKind.String ? i.GetString()! : string.Empty;
            var minRole = permission.TryGetProperty("minRole", out var m) && m.ValueKind == JsonValueKind.String ? m.GetString()! : string.Empty;
            if (!NamePattern().IsMatch(id))
            {
                _problems.Add($"permissions: '{id}' is not a valid permission id.");
            }

            if (!Roles.Contains(minRole))
            {
                _problems.Add($"permissions['{id}'].minRole must be Guest, Member, Admin or Owner.");
            }

            permissions.Add(new AddonPermission(id, minRole));
        }

        if (permissions.Select(p => p.Id).Distinct(StringComparer.Ordinal).Count() != permissions.Count)
        {
            _problems.Add("permissions: every id must be different.");
        }

        return permissions;
    }

    private void CheckHtmlPath(string field, string path)
    {
        if (path.Length == 0)
        {
            return;
        }

        if (!PathPattern().IsMatch(path) || path.StartsWith('/') || path.Contains("..", StringComparison.Ordinal)
            || !path.EndsWith(".html", StringComparison.Ordinal))
        {
            _problems.Add($"{field} must be a relative path to an .html file in the package.");
        }
    }

    private int? Int(string property) =>
        root.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number)
            ? number
            : null;

    private string? String(string property, bool required)
    {
        if (root.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String && value.GetString()!.Length > 0)
        {
            return value.GetString();
        }

        if (required)
        {
            _problems.Add($"{property} is required.");
        }

        return null;
    }

    private LocalizedText? Text(JsonElement owner, string property, bool required, string prefix = "")
    {
        if (owner.TryGetProperty(property, out var value))
        {
            var pl = value.ValueKind == JsonValueKind.Object && value.TryGetProperty("pl", out var p) && p.ValueKind == JsonValueKind.String
                ? p.GetString()
                : null;
            var en = value.ValueKind == JsonValueKind.Object && value.TryGetProperty("en", out var e) && e.ValueKind == JsonValueKind.String
                ? e.GetString()
                : null;
            if (!string.IsNullOrWhiteSpace(pl) && pl.Length <= 200 && (en is null || en.Length <= 200))
            {
                return new LocalizedText(pl, string.IsNullOrWhiteSpace(en) ? pl : en);
            }

            _problems.Add($"{prefix}{property} needs a Polish text (pl) of at most 200 characters, and optionally en.");
            return null;
        }

        if (required)
        {
            _problems.Add($"{prefix}{property} is required.");
        }

        return null;
    }

    private List<string> Strings(string property) =>
        root.TryGetProperty(property, out var value) ? StringArray(value, property) : [];

    private List<string> StringArray(JsonElement value, string field)
    {
        if (value.ValueKind != JsonValueKind.Array)
        {
            _problems.Add($"{field} must be an array.");
            return [];
        }

        var items = new List<string>();
        foreach (var item in value.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.String)
            {
                items.Add(item.GetString()!);
            }
            else
            {
                _problems.Add($"{field} may hold only strings.");
            }
        }

        return items;
    }

    private List<JsonElement> Objects(JsonElement value, string field)
    {
        if (value.ValueKind != JsonValueKind.Array)
        {
            _problems.Add($"{field} must be an array.");
            return [];
        }

        var items = value.EnumerateArray().Where(i => i.ValueKind == JsonValueKind.Object).ToList();
        if (items.Count != value.GetArrayLength())
        {
            _problems.Add($"{field} may hold only objects.");
        }

        return items;
    }

    private static bool IsHttpsOrigin(string origin) =>
        !origin.Contains('*', StringComparison.Ordinal)
        && Uri.TryCreate(origin, UriKind.Absolute, out var uri)
        && uri.Scheme == Uri.UriSchemeHttps
        && string.IsNullOrEmpty(uri.UserInfo)
        && uri.AbsolutePath == "/"
        && string.IsNullOrEmpty(uri.Query)
        && string.IsNullOrEmpty(uri.Fragment)
        && origin.TrimEnd('/').Equals($"{uri.Scheme}://{uri.Authority}", StringComparison.OrdinalIgnoreCase);
}
