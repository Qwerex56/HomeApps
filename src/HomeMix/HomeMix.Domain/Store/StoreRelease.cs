namespace HomeMix.Domain.Store;

/// <summary>What GitHub says about a repository's latest release besides the package: its tag, date and the README.</summary>
public sealed record StoreRelease(string Tag, DateTimeOffset? PublishedAt, string? Readme);
