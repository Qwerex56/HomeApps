using System.Text.RegularExpressions;

namespace HomeApps.Plugins.Manifest;

/// <summary>Checks a manifest when the plugin starts, so a broken one never reaches the web app.</summary>
public static partial class PluginManifestValidator
{
    /// <summary>First path segments the core services and the web app already use.</summary>
    private static readonly string[] ReservedIds = ["auth", "users", "admin", "households", "invites", "homes", "join", "wall", "account", "openapi"];

    public static IReadOnlyList<string> Validate(PluginManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        var problems = new List<string>();

        if (manifest.Id is null || !IdPattern().IsMatch(manifest.Id))
        {
            problems.Add("Id must be 2-32 lower-case letters, digits or '-', starting with a letter.");
        }
        else if (ReservedIds.Contains(manifest.Id))
        {
            problems.Add($"Id '{manifest.Id}' is used by HomeApps itself.");
        }

        if (!IsVersion(manifest.Version))
        {
            problems.Add("Version must be a semantic version such as 1.0.0.");
        }

        if (!IsVersion(manifest.MinHomeAppsVersion))
        {
            problems.Add("MinHomeAppsVersion must be a semantic version such as 2.1.0.");
        }

        CheckText(problems, "Name", manifest.Name);
        if (manifest.Description is not null)
        {
            CheckText(problems, "Description", manifest.Description);
        }

        if (string.IsNullOrWhiteSpace(manifest.Icon))
        {
            problems.Add("Icon is required.");
        }

        if (manifest.Scopes is null || manifest.Scopes.Count == 0)
        {
            problems.Add("Declare at least one scope.");
        }
        else
        {
            Unique(problems, "scope", manifest.Scopes.Select(s => s.ToString()));
            if (manifest.Enablement is null && manifest.Scopes.Any(s => s is DataScope.Household or DataScope.HouseholdUser))
            {
                problems.Add("A plugin with household data needs Enablement: who switches it on in a household.");
            }
        }

        foreach (var permission in manifest.Permissions)
        {
            if (!PermissionPattern().IsMatch(permission.Id))
            {
                problems.Add($"Permission '{permission.Id}' must be lower-case words separated by '.', e.g. tasks.write.");
            }
        }

        Unique(problems, "permission", manifest.Permissions.Select(p => p.Id));

        if (manifest.Ui is not null)
        {
            Unique(problems, "route", manifest.Ui.Routes.Select(r => r.Path));
            Unique(problems, "widget", manifest.Ui.Widgets.Select(w => w.Id));
            foreach (var route in manifest.Ui.Routes.Where(r => r.Path.StartsWith('/')))
            {
                problems.Add($"Route '{route.Path}' must be relative (no leading '/').");
            }

            foreach (var widget in manifest.Ui.Widgets.Where(w => w.Places.Count == 0))
            {
                problems.Add($"Widget '{widget.Id}' needs at least one place.");
            }
        }

        Unique(problems, "display screen", manifest.Display.Select(d => d.Id));
        foreach (var screen in manifest.Display)
        {
            if (string.IsNullOrWhiteSpace(screen.Path) || screen.Path.StartsWith('/'))
            {
                problems.Add($"Display screen '{screen.Id}' needs a path relative to the API (no leading '/').");
            }

            if (screen.RefreshSeconds < 60)
            {
                problems.Add($"Display screen '{screen.Id}' must refresh at most once a minute.");
            }
        }

        if (manifest.Mcp is not null)
        {
            if (manifest.Mcp.Tools.Count == 0)
            {
                problems.Add("An MCP server needs at least one tool.");
            }

            Unique(problems, "MCP tool", manifest.Mcp.Tools);
        }

        return problems;
    }

    /// <summary>Throws with every problem at once.</summary>
    public static void EnsureValid(PluginManifest manifest)
    {
        var problems = Validate(manifest);
        if (problems.Count > 0)
        {
            throw new InvalidOperationException(
                $"The manifest of plugin '{manifest.Id}' is invalid:{Environment.NewLine}- {string.Join($"{Environment.NewLine}- ", problems)}");
        }
    }

    private static bool IsVersion(string? value) => value is not null && VersionPattern().IsMatch(value);

    private static void CheckText(List<string> problems, string field, LocalizedText? text)
    {
        if (text is null || string.IsNullOrWhiteSpace(text.Pl) || string.IsNullOrWhiteSpace(text.En))
        {
            problems.Add($"{field} needs both a Polish and an English text.");
        }
    }

    private static void Unique(List<string> problems, string what, IEnumerable<string> values)
    {
        foreach (var duplicate in values.GroupBy(v => v, StringComparer.Ordinal).Where(g => g.Count() > 1))
        {
            problems.Add($"Duplicate {what} '{duplicate.Key}'.");
        }
    }

    [GeneratedRegex("^[a-z][a-z0-9-]{1,31}$")]
    private static partial Regex IdPattern();

    [GeneratedRegex(@"^[a-z][a-z0-9_]*(\.[a-z][a-z0-9_]*)*$")]
    private static partial Regex PermissionPattern();

    [GeneratedRegex(@"^\d+\.\d+\.\d+(-[0-9A-Za-z.-]+)?$")]
    private static partial Regex VersionPattern();
}
