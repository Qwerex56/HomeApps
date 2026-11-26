namespace Accounts.Application.Admin.GetServerAbout;

/// <param name="Status"><c>healthy</c> or <c>unavailable</c>.</param>
public sealed record PluginAboutView(string Id, string? Version, string Status);
