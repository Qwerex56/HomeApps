namespace HomeApps.Plugins;

/// <summary>The nickname in the household, or else the person's display name.</summary>
public sealed record MemberName(Guid UserId, string Name);
