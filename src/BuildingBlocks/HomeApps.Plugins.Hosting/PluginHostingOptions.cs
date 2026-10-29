using System.ComponentModel.DataAnnotations;

namespace HomeApps.Plugins.Hosting;

/// <summary>Configuration section <c>Plugin</c>.</summary>
public sealed class PluginHostingOptions
{
    public const string SectionName = "Plugin";

    /// <summary>Households inside the Docker network; from Rider during development <c>http://localhost:5300</c>.</summary>
    [Required]
    [Url]
    public string HouseholdsUrl { get; set; } = "http://households:8080";

    /// <summary>How long a member's role is trusted without asking Households again (K2: 30-60 s).</summary>
    [Range(1, 600)]
    public int AccessCacheSeconds { get; set; } = 60;

    /// <summary>How long "no access" is remembered. Short, so someone who just joined gets in almost at once.</summary>
    [Range(0, 60)]
    public int NoAccessCacheSeconds { get; set; } = 5;
}
