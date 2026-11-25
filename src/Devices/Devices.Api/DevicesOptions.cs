using System.ComponentModel.DataAnnotations;

namespace Devices.Api;

/// <summary>Configuration section <c>Devices</c>.</summary>
public sealed class DevicesOptions
{
    public const string SectionName = "Devices";

    /// <summary>
    /// JARVIS's resource (<c>https://home-apps.dev/jarvis/mcp</c>): a token an app got from "Połączone aplikacje" has it
    /// as <c>aud</c>, and the same app links its device here, so it is accepted next to <c>homeapps</c>. Empty: only <c>homeapps</c>.
    /// </summary>
    public string Resource { get; set; } = string.Empty;

    /// <summary>How long JARVIS waits for a device to answer an action.</summary>
    public TimeSpan ActionTimeout { get; set; } = TimeSpan.FromSeconds(20);

    /// <summary>Devices' own service client secret for Auth and Households (<c>X-Client-Secret</c>); they list it. Devices does not start without it.</summary>
    [Required(AllowEmptyStrings = false, ErrorMessage = "Missing required secret: DEVICES_CLIENT_SECRET")]
    public string ClientSecret { get; set; } = string.Empty;
}
