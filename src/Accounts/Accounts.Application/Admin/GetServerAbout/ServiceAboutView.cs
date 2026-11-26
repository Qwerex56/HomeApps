namespace Accounts.Application.Admin.GetServerAbout;

/// <param name="Version">Null when the service did not answer.</param>
public sealed record ServiceAboutView(string Name, string? Version, int AppliedMigrations, string? LastMigration);
