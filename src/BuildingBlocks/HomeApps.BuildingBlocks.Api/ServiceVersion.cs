namespace HomeApps.BuildingBlocks.Api;

/// <summary>The body of <c>GET /health/version</c>: the service, its HomeApps version and its applied migrations.</summary>
public sealed record ServiceVersion(string Service, string Version, IReadOnlyList<string> Migrations);
