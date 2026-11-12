using System.ComponentModel.DataAnnotations;

namespace HomeMix.Application;

/// <summary>Configuration section <c>HomeMix</c> (docs/homemix.md): what the instance's host decides.</summary>
public sealed class HomeMixOptions
{
    public const string SectionName = "HomeMix";

    /// <summary>Off: nobody adds or updates add-ons; installed ones keep working.</summary>
    public bool UploadsEnabled { get; set; } = true;

    /// <summary>Add-on ids or package SHA-256s the host blocked: they cannot be added and stop being served.</summary>
    public IList<string> Blocked { get; } = [];

    /// <summary>The instance's plugins (<c>HOMEAPPS_PLUGINS</c>, comma separated); an add-on may not take their ids.</summary>
    public string Plugins { get; set; } = string.Empty;

    [Range(64 * 1024, 50 * 1024 * 1024)]
    public long MaxPackageBytes { get; set; } = 5 * 1024 * 1024;

    /// <summary>One data document (docs/homemix.md, "Data").</summary>
    [Range(1024, 16 * 1024 * 1024)]
    public int MaxDocumentBytes { get; set; } = 1024 * 1024;

    /// <summary>All data of one installation, both scopes.</summary>
    [Range(64 * 1024, 1024L * 1024 * 1024)]
    public long MaxInstallationBytes { get; set; } = 10 * 1024 * 1024;

    /// <summary>Documents in one collection of one space.</summary>
    [Range(1, 100_000)]
    public int MaxDocumentsPerCollection { get; set; } = 1000;

    /// <summary>Notifications one installation may send in an hour (<c>notify.push</c>).</summary>
    [Range(1, 10_000)]
    public int NotificationsPerHour { get; set; } = 30;

    /// <summary>Where the zips are kept, one per SHA-256 (the <c>homemix-packages</c> volume).</summary>
    [Required]
    public string PackagesPath { get; set; } = "/data/packages";

    [Required]
    [Url]
    public string GitHubApiUrl { get; set; } = "https://api.github.com";

    /// <summary>Households inside the Docker network; from Rider during development <c>http://localhost:5300</c>.</summary>
    [Required]
    [Url]
    public string HouseholdsUrl { get; set; } = "http://households:8080";

    /// <summary>
    /// The store's index (W9): a public <c>addons.json</c> with repository links, added to by pull request. Empty: the
    /// store shows only the official add-ons.
    /// </summary>
    public string StoreIndexUrl { get; set; } = "https://raw.githubusercontent.com/Qwerex56/HomeApps/master/homemix/store/addons.json";

    /// <summary>How long the store keeps what it read from GitHub.</summary>
    [Range(1, 24 * 60)]
    public int StoreCacheMinutes { get; set; } = 60;

    /// <summary>The official add-ons' zips built into the image (<c>homemix/official/*</c>); missing: none.</summary>
    public string OfficialPath { get; set; } = "/app/official";

    /// <summary>Optional, only to raise GitHub's limit of 60 anonymous requests an hour.</summary>
    public string? GitHubToken { get; set; }

    public IReadOnlySet<string> PluginIds =>
        Plugins.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToHashSet(StringComparer.Ordinal);

    public bool IsBlocked(string addonId, string sha256) =>
        Blocked.Any(b => string.Equals(b, addonId, StringComparison.Ordinal) || string.Equals(b, sha256, StringComparison.OrdinalIgnoreCase));
}
